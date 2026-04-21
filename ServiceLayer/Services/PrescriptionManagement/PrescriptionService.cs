using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.DTOs.Prescription.Request;
using ServiceLayer.DTOs.Prescription.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.PrescriptionManagement;

public class PrescriptionService(
    IUnitOfWork unitOfWork,
    OnlineEyewearDbContext dbContext,
    IPrescriptionPricingService prescriptionPricingService) : IPrescriptionService
{
    private static readonly HashSet<PrescriptionStatus> StaffReviewTargetStatuses =
    [
        PrescriptionStatus.Reviewing,
        PrescriptionStatus.Approved,
        PrescriptionStatus.Rejected,
        PrescriptionStatus.InProduction
    ];

    private static readonly Dictionary<PrescriptionStatus, HashSet<PrescriptionStatus>> AllowedStatusTransitions = new()
    {
        [PrescriptionStatus.Submitted] =
        [
            PrescriptionStatus.Reviewing,
            PrescriptionStatus.NeedMoreInfo,
            PrescriptionStatus.Approved,
            PrescriptionStatus.Rejected
        ],
        [PrescriptionStatus.Reviewing] =
        [
            PrescriptionStatus.NeedMoreInfo,
            PrescriptionStatus.Approved,
            PrescriptionStatus.Rejected
        ],
        [PrescriptionStatus.NeedMoreInfo] =
        [
            PrescriptionStatus.Submitted,
            PrescriptionStatus.Reviewing,
            PrescriptionStatus.Rejected
        ],
        [PrescriptionStatus.Approved] =
        [
            PrescriptionStatus.InProduction
        ],
        [PrescriptionStatus.Rejected] = [],
        [PrescriptionStatus.InProduction] = []
    };

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;
    private readonly IPrescriptionPricingService _prescriptionPricingService = prescriptionPricingService;

    public async Task<PagedResult<PrescriptionListItemResponse>> GetPrescriptionsAsync(
        GetPrescriptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.PrescriptionSpecs
            .AsNoTracking()
            .Include(prescription => prescription.OrderItems)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.PrescriptionStatus))
        {
            if (!ApiEnumMapper.TryParsePrescriptionStatus(request.PrescriptionStatus, out var prescriptionStatus))
            {
                throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid prescription query");
            }

            query = query.Where(prescription => prescription.PrescriptionStatus == prescriptionStatus);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(prescription => prescription.UserId == request.UserId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;
            query = query.Where(prescription => prescription.CreatedAt >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDateExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(prescription => prescription.CreatedAt < toDateExclusive);
        }

        var page = Math.Max(request.Page, PaginationRequest.DefaultPage);
        var pageSize = request.PageSize < 1
            ? PaginationRequest.DefaultPageSize
            : Math.Min(request.PageSize, PaginationRequest.MaxPageSize);
        var sortDescending = ParseSortOrder(request.SortOrder);

        query = ApplySorting(query, request.SortBy, sortDescending);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(prescription => new PrescriptionListItemResponse
            {
                PrescriptionId = prescription.PrescriptionId,
                OrderId = prescription.OrderItems
                    .OrderBy(item => item.OrderItemId)
                    .Select(item => (int?)item.OrderId)
                    .FirstOrDefault(),
                PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(prescription.PrescriptionStatus)
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PrescriptionListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<PrescriptionDetailResponse?> GetPrescriptionByIdAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _dbContext.PrescriptionSpecs
            .AsNoTracking()
            .Include(current => current.OrderItems)
            .Include(current => current.LensType)
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);

        return prescription is null
            ? null
            : new PrescriptionDetailResponse
            {
                PrescriptionId = prescription.PrescriptionId,
                UserId = prescription.UserId,
                OrderId = prescription.OrderItems
                    .OrderBy(item => item.OrderItemId)
                    .Select(item => (int?)item.OrderId)
                    .FirstOrDefault(),
                LensTypeId = prescription.LensTypeId,
                LensTypeCode = prescription.LensTypeCode ?? prescription.LensType?.LensCode,
                LensMaterial = prescription.LensMaterial,
                Coatings = DeserializeCoatings(prescription.Coatings).ToList(),
                LensBasePrice = prescription.LensBasePrice,
                MaterialPrice = prescription.MaterialPrice,
                CoatingPrice = prescription.CoatingPrice,
                TotalLensPrice = prescription.TotalLensPrice,
                RightEye = new PrescriptionEyeResponse
                {
                    Sph = prescription.SphRight,
                    Cyl = prescription.CylRight,
                    Axis = prescription.AxisRight
                },
                LeftEye = new PrescriptionEyeResponse
                {
                    Sph = prescription.SphLeft,
                    Cyl = prescription.CylLeft,
                    Axis = prescription.AxisLeft
                },
                Pd = prescription.Pd,
                PrescriptionImageUrl = prescription.PrescriptionImage,
                PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(prescription.PrescriptionStatus),
                StaffId = prescription.StaffId,
                VerifiedAt = prescription.VerifiedAt,
                Notes = prescription.Notes,
                CreatedAt = prescription.CreatedAt
            };
    }

    public async Task<PrescriptionStatusResponse> ReviewPrescriptionAsync(
        int staffUserId,
        int prescriptionId,
        ReviewPrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ApiEnumMapper.TryParsePrescriptionStatus(request.PrescriptionStatus, out var prescriptionStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }

        if (!StaffReviewTargetStatuses.Contains(prescriptionStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }

        var prescription = await GetTrackedPrescriptionAsync(prescriptionId, cancellationToken);

        if (prescription is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PRESCRIPTION_NOT_FOUND", "Prescription not found");
        }

        ValidateStatusTransition(prescription.PrescriptionStatus, prescriptionStatus);

        var now = DateTime.UtcNow;
        prescription.PrescriptionStatus = prescriptionStatus;
        prescription.StaffId = staffUserId;
        prescription.Notes = NormalizeOptionalNote(request.Notes);
        prescription.VerifiedAt = prescriptionStatus is PrescriptionStatus.Approved or PrescriptionStatus.Rejected or PrescriptionStatus.InProduction
            ? now
            : null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PrescriptionStatusResponse
        {
            Message = "Prescription reviewed",
            PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(prescriptionStatus)
        };
    }

    public async Task<PrescriptionStatusResponse> RequestMoreInfoAsync(
        int staffUserId,
        int prescriptionId,
        RequestMorePrescriptionInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notes = NormalizeOptionalNote(request.Notes);

        if (notes is null)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }

        var prescription = await GetTrackedPrescriptionAsync(prescriptionId, cancellationToken);

        if (prescription is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PRESCRIPTION_NOT_FOUND", "Prescription not found");
        }

        ValidateStatusTransition(prescription.PrescriptionStatus, PrescriptionStatus.NeedMoreInfo);

        prescription.PrescriptionStatus = PrescriptionStatus.NeedMoreInfo;
        prescription.StaffId = staffUserId;
        prescription.Notes = notes;
        prescription.VerifiedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PrescriptionStatusResponse
        {
            Message = "More info requested",
            PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(PrescriptionStatus.NeedMoreInfo)
        };
    }

    public async Task<PrescriptionStatusResponse> ResubmitPrescriptionAsync(
        int userId,
        int prescriptionId,
        ResubmitPrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prescription = await _dbContext.PrescriptionSpecs
            .Include(current => current.LensType)
            .FirstOrDefaultAsync(
                current => current.PrescriptionId == prescriptionId && current.UserId == userId,
                cancellationToken);

        if (prescription is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PRESCRIPTION_NOT_FOUND", "Prescription not found");
        }

        if (prescription.PrescriptionStatus != PrescriptionStatus.NeedMoreInfo)
        {
            throw CreateApiException(
                HttpStatusCode.Conflict,
                "PRESCRIPTION_RESUBMIT_NOT_ALLOWED",
                "Prescription resubmission is only allowed when status is needMoreInfo");
        }

        if (prescription.LensType is null || !prescription.LensType.IsActive)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_INPUT", "Invalid prescription input");
        }

        ValidateStatusTransition(prescription.PrescriptionStatus, PrescriptionStatus.Submitted);

        var preparedRequest = PrepareManualPrescriptionInput(request);
        var recalculatedPricing = _prescriptionPricingService.Calculate(
            framePrice: 0m,
            lensBasePrice: prescription.LensType.Price,
            lensMaterial: prescription.LensMaterial,
            coatings: DeserializeCoatings(prescription.Coatings),
            quantity: 1,
            errorCode: "INVALID_PRESCRIPTION_INPUT",
            errorMessage: "Invalid prescription input");

        prescription.SphRight = preparedRequest.RightSph;
        prescription.CylRight = preparedRequest.RightCyl;
        prescription.AxisRight = preparedRequest.RightAxis;
        prescription.SphLeft = preparedRequest.LeftSph;
        prescription.CylLeft = preparedRequest.LeftCyl;
        prescription.AxisLeft = preparedRequest.LeftAxis;
        prescription.Pd = preparedRequest.Pd;
        prescription.PrescriptionImage = preparedRequest.PrescriptionImageUrl;
        prescription.Notes = preparedRequest.Notes;
        prescription.LensTypeCode = prescription.LensType.LensCode;
        prescription.LensMaterial = recalculatedPricing.LensMaterial;
        prescription.Coatings = SerializeCoatings(recalculatedPricing.Coatings);
        prescription.LensBasePrice = recalculatedPricing.LensBasePrice;
        prescription.MaterialPrice = recalculatedPricing.MaterialPrice;
        prescription.CoatingPrice = recalculatedPricing.CoatingPrice;
        prescription.TotalLensPrice = recalculatedPricing.LensPrice;
        prescription.PrescriptionStatus = PrescriptionStatus.Submitted;
        prescription.StaffId = null;
        prescription.VerifiedAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PrescriptionStatusResponse
        {
            Message = "Prescription resubmitted",
            PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(PrescriptionStatus.Submitted)
        };
    }

    private Task<PrescriptionSpec?> GetTrackedPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken)
    {
        return _dbContext.PrescriptionSpecs
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);
    }

    private static PreparedPrescriptionInput PrepareManualPrescriptionInput(ResubmitPrescriptionRequest request)
    {
        var rightEye = request.RightEye;
        var leftEye = request.LeftEye;

        if (rightEye?.Sph is null
            || rightEye.Cyl is null
            || rightEye.Axis is null
            || leftEye?.Sph is null
            || leftEye.Cyl is null
            || leftEye.Axis is null
            || request.Pd is null)
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Manual prescription input is required",
                new { field = "manualPrescription", issue = "Manual prescription input is required" });
        }

        ValidateAxisRange(rightEye.Axis.Value, "rightEye.axis");
        ValidateAxisRange(leftEye.Axis.Value, "leftEye.axis");

        if (request.Pd.Value <= 0)
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Invalid prescription input",
                new { field = "pd", issue = "pd must be greater than 0" });
        }

        var notes = NormalizeOptionalNote(request.Notes);
        var prescriptionImageUrl = NormalizePrescriptionImageReference(request.PrescriptionImageUrl);

        return new PreparedPrescriptionInput(
            rightEye.Sph.Value,
            rightEye.Cyl.Value,
            rightEye.Axis.Value,
            leftEye.Sph.Value,
            leftEye.Cyl.Value,
            leftEye.Axis.Value,
            request.Pd.Value,
            notes,
            prescriptionImageUrl);
    }

    private static void ValidateStatusTransition(PrescriptionStatus currentStatus, PrescriptionStatus nextStatus)
    {
        if (currentStatus == nextStatus)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }

        if (!AllowedStatusTransitions.TryGetValue(currentStatus, out var allowedStatuses)
            || !allowedStatuses.Contains(nextStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }
    }

    private static void ValidateAxisRange(int axis, string field)
    {
        if (axis is < 0 or > 180)
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Invalid prescription input",
                new { field, issue = $"{field} must be between 0 and 180" });
        }
    }

    private static IReadOnlyList<string> DeserializeCoatings(string? serializedCoatings)
    {
        if (string.IsNullOrWhiteSpace(serializedCoatings))
        {
            return [];
        }

        return serializedCoatings
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? SerializeCoatings(IReadOnlyCollection<string>? coatings)
    {
        if (coatings is null || coatings.Count == 0)
        {
            return null;
        }

        var serialized = string.Join(",", coatings);

        if (serialized.Length > 500)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_INPUT", "Invalid prescription input");
        }

        return serialized;
    }

    private static string? NormalizePrescriptionImageReference(string? value)
    {
        var normalized = NormalizeText(value);

        if (normalized is not null
            && normalized.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Invalid prescription input",
                new { field = "prescriptionImageUrl", issue = "prescriptionImageUrl must be an uploaded image URL/path, not raw image data" });
        }

        if (normalized is not null && normalized.Length > 500)
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Invalid prescription input",
                new { field = "prescriptionImageUrl", issue = "prescriptionImageUrl must not exceed 500 characters" });
        }

        return normalized;
    }

    private static string? NormalizeOptionalNote(string? value)
    {
        var normalized = NormalizeText(value);

        if (normalized is not null && normalized.Length > 255)
        {
            throw CreateApiException(
                HttpStatusCode.BadRequest,
                "INVALID_PRESCRIPTION_INPUT",
                "Invalid prescription input",
                new { field = "notes", issue = "notes must not exceed 255 characters" });
        }

        return normalized;
    }

    private static IOrderedQueryable<PrescriptionSpec> ApplySorting(
        IQueryable<PrescriptionSpec> query,
        string? sortBy,
        bool descending)
    {
        var normalizedSortBy = NormalizeSortField(sortBy);

        return normalizedSortBy switch
        {
            null or "createdat" => descending
                ? query.OrderByDescending(prescription => prescription.CreatedAt).ThenByDescending(prescription => prescription.PrescriptionId)
                : query.OrderBy(prescription => prescription.CreatedAt).ThenBy(prescription => prescription.PrescriptionId),
            "prescriptionid" => descending
                ? query.OrderByDescending(prescription => prescription.PrescriptionId)
                : query.OrderBy(prescription => prescription.PrescriptionId),
            "userid" => descending
                ? query.OrderByDescending(prescription => prescription.UserId).ThenByDescending(prescription => prescription.PrescriptionId)
                : query.OrderBy(prescription => prescription.UserId).ThenBy(prescription => prescription.PrescriptionId),
            "orderid" => descending
                ? query.OrderByDescending(prescription => prescription.OrderItems.Min(item => (int?)item.OrderId)).ThenByDescending(prescription => prescription.PrescriptionId)
                : query.OrderBy(prescription => prescription.OrderItems.Min(item => (int?)item.OrderId)).ThenBy(prescription => prescription.PrescriptionId),
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid prescription query")
        };
    }

    private static bool ParseSortOrder(string? sortOrder)
    {
        var normalizedSortOrder = NormalizeSortField(sortOrder);

        return normalizedSortOrder switch
        {
            null or "desc" => true,
            "asc" => false,
            _ => throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_QUERY", "Invalid prescription query")
        };
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static string? NormalizeSortField(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Trim()
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string errorCode, string message, object? details = null)
    {
        return new ApiException((int)statusCode, errorCode, message, details);
    }

    private sealed record PreparedPrescriptionInput(
        decimal RightSph,
        decimal RightCyl,
        int RightAxis,
        decimal LeftSph,
        decimal LeftCyl,
        int LeftAxis,
        decimal Pd,
        string? Notes,
        string? PrescriptionImageUrl);
}
