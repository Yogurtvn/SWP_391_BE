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
    OnlineEyewearDbContext dbContext) : IPrescriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;

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
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);

        return prescription is null
            ? null
            : new PrescriptionDetailResponse
            {
                PrescriptionId = prescription.PrescriptionId,
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
                PrescriptionImageUrl = prescription.PrescriptionImage
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

        var prescription = await GetTrackedPrescriptionAsync(prescriptionId, cancellationToken);

        if (prescription is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PRESCRIPTION_NOT_FOUND", "Prescription not found");
        }

        var now = DateTime.UtcNow;
        prescription.PrescriptionStatus = prescriptionStatus;
        prescription.StaffId = staffUserId;
        prescription.Notes = NormalizeText(request.Notes);
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

        var notes = NormalizeText(request.Notes);

        if (notes is null)
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
        }

        var prescription = await GetTrackedPrescriptionAsync(prescriptionId, cancellationToken);

        if (prescription is null)
        {
            throw CreateApiException(HttpStatusCode.NotFound, "PRESCRIPTION_NOT_FOUND", "Prescription not found");
        }

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

    private Task<PrescriptionSpec?> GetTrackedPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken)
    {
        return _dbContext.PrescriptionSpecs
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);
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
}
