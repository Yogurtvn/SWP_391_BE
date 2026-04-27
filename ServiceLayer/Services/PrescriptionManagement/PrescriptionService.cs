using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Data;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Notifications;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.DTOs.Prescription.Request;
using ServiceLayer.DTOs.Prescription.Response;
using ServiceLayer.Exceptions;
using ServiceLayer.Utilities;
using System.Net;

namespace ServiceLayer.Services.PrescriptionManagement;

/// <summary>
/// Dịch vụ quản lý đơn thuốc (Prescription), bao gồm tra cứu, xem chi tiết và phê duyệt đơn thuốc.
/// </summary>
public class PrescriptionService(
    IUnitOfWork unitOfWork,
    OnlineEyewearDbContext dbContext,
    IPreOrderBackInStockNotificationService backInStockNotificationService) : IPrescriptionService
{
    private static readonly HashSet<PrescriptionStatus> StaffReviewTargetStatuses =
    [
        PrescriptionStatus.Reviewing,
        PrescriptionStatus.Approved,
        PrescriptionStatus.Rejected
    ];

    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OnlineEyewearDbContext _dbContext = dbContext;
    private readonly IPreOrderBackInStockNotificationService _backInStockNotificationService = backInStockNotificationService;

    /// <summary>
    /// Lấy danh sách các đơn thuốc với bộ lọc và phân trang.
    /// </summary>
    public async Task<PagedResult<PrescriptionListItemResponse>> GetPrescriptionsAsync(
        GetPrescriptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.PrescriptionSpecs
            .AsNoTracking()
            .Include(prescription => prescription.OrderItems)
            .Include(prescription => prescription.User)
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
        // Thực hiện phân trang: Skip bỏ qua các bản ghi trang trước, Take lấy số bản ghi trang hiện tại
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(prescription => new PrescriptionListItemResponse
            {
                PrescriptionId = prescription.PrescriptionId,
                UserId = prescription.UserId,
                CustomerName = prescription.User.FullName,
                CustomerEmail = prescription.User.Email,
                // Lấy OrderId đầu tiên liên quan đến đơn thuốc này (nếu có)
                OrderId = prescription.OrderItems
                    .OrderBy(item => item.OrderItemId)
                    .Select(item => (int?)item.OrderId)
                    .FirstOrDefault(),
                LensTypeId = prescription.LensTypeId,
                LensTypeCode = prescription.LensTypeCode,
                LensMaterial = prescription.LensMaterial,
                TotalLensPrice = prescription.TotalLensPrice,
                PrescriptionImageUrl = prescription.PrescriptionImage,
                // Chuyển đổi Enum từ Database sang chuỗi API thân thiện cho Frontend
                PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(prescription.PrescriptionStatus),
                Notes = prescription.Notes,
                CreatedAt = prescription.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Trả về đối tượng PagedResult chứa cả dữ liệu và thông tin phân trang (Tổng số trang, HasNextPage,...)
        return PagedResult<PrescriptionListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một đơn thuốc cụ thể theo ID.
    /// </summary>
    public async Task<PrescriptionDetailResponse?> GetPrescriptionByIdAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _dbContext.PrescriptionSpecs
            .AsNoTracking()
            .Include(current => current.OrderItems)
            .Include(current => current.LensType)
            .Include(current => current.User)
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);

        return prescription is null
            ? null
            : new PrescriptionDetailResponse
            {
                PrescriptionId = prescription.PrescriptionId,
                UserId = prescription.UserId,
                CustomerName = prescription.User.FullName,
                CustomerEmail = prescription.User.Email,
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

    /// <summary>
    /// Nhân viên thực hiện duyệt hoặc từ chối đơn thuốc. 
    /// Nếu từ chối, các đơn hàng liên quan sẽ bị hủy tự động.
    /// </summary>
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

        ValidatePrescriptionStatusTransition(prescription.PrescriptionStatus, prescriptionStatus);
        IReadOnlyCollection<OrderWorkflowMutations.InventoryQuantityTransition> inventoryTransitions =
            Array.Empty<OrderWorkflowMutations.InventoryQuantityTransition>();

        try
        {
            // Bắt đầu một Transaction để đảm bảo tính toàn vẹn dữ liệu: 
            // Nếu cập nhật trạng thái đơn thuốc thành công nhưng hủy đơn hàng lỗi thì cả quá trình sẽ bị hủy bỏ (Rollback).
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;
            prescription.PrescriptionStatus = prescriptionStatus; // Cập nhật trạng thái mới (Approved/Rejected)
            prescription.StaffId = staffUserId; // Ghi nhận nhân viên nào thực hiện duyệt
            prescription.Notes = NormalizeOptionalNote(request.Notes);
            prescription.VerifiedAt = prescriptionStatus is PrescriptionStatus.Approved or PrescriptionStatus.Rejected
                ? now
                : null;

            // Lưu thay đổi trạng thái đơn thuốc vào DB
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (prescriptionStatus == PrescriptionStatus.Rejected)
            {
                // LUỒNG QUAN TRỌNG: Nếu đơn thuốc bị từ chối, hệ thống tự động tìm và hủy các đơn hàng liên quan.
                // Điều này giúp tránh việc đơn hàng vẫn ở trạng thái "Chờ xử lý" khi đơn thuốc đã bị bác bỏ.
                inventoryTransitions = await CancelOrdersForRejectedPrescriptionAsync(
                    prescriptionId,
                    staffUserId,
                    cancellationToken);
            }

            // Xác nhận hoàn tất tất cả các thay đổi vào Database
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await NotifyBackInStockTransitionsAsync(inventoryTransitions, "prescription:rejected", cancellationToken);

        return new PrescriptionStatusResponse
        {
            Message = "Prescription reviewed",
            PrescriptionStatus = ApiEnumMapper.ToApiPrescriptionStatus(prescriptionStatus)
        };
    }

    public Task<PrescriptionStatusResponse> RequestMoreInfoAsync(
        int staffUserId,
        int prescriptionId,
        RequestMorePrescriptionInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = staffUserId;
        _ = prescriptionId;
        ArgumentNullException.ThrowIfNull(request);
        _ = cancellationToken;

        throw CreateApiException(
            HttpStatusCode.Gone,
            "PRESCRIPTION_FLOW_DEPRECATED",
            "request-more-info flow has been deprecated");
    }

    public Task<PrescriptionStatusResponse> ResubmitPrescriptionAsync(
        int userId,
        int prescriptionId,
        ResubmitPrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = prescriptionId;
        ArgumentNullException.ThrowIfNull(request);
        _ = cancellationToken;

        throw CreateApiException(
            HttpStatusCode.Gone,
            "PRESCRIPTION_FLOW_DEPRECATED",
            "resubmit flow has been deprecated");
    }

    private Task<PrescriptionSpec?> GetTrackedPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken)
    {
        return _dbContext.PrescriptionSpecs
            .FirstOrDefaultAsync(current => current.PrescriptionId == prescriptionId, cancellationToken);
    }

    private async Task<IReadOnlyCollection<OrderWorkflowMutations.InventoryQuantityTransition>> CancelOrdersForRejectedPrescriptionAsync(
        int prescriptionId,
        int staffUserId,
        CancellationToken cancellationToken)
    {
        // Tìm tất cả đơn hàng thuộc loại "Kính thuốc" (OrderType.Prescription) có chứa đơn thuốc bị từ chối này
        var orders = await _dbContext.Orders
            .Include(current => current.OrderItems)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant.Inventory)
            .Include(current => current.Payments)
                .ThenInclude(payment => payment.PaymentHistories)
            .Include(current => current.OrderStatusHistories)
            .Where(current =>
                current.OrderType == OrderType.Prescription
                && current.OrderItems.Any(item => item.PrescriptionId == prescriptionId))
            .ToListAsync(cancellationToken);

        var mergedTransitions = new Dictionary<int, OrderWorkflowMutations.InventoryQuantityTransition>();

        foreach (var order in orders)
        {
            // Kiểm tra chính sách nghiệp vụ (Workflow Policy) xem đơn hàng hiện tại có được phép chuyển sang trạng thái "Cancelled" không
            if (!OrderWorkflowPolicies.CanTransitionOrderStatus(
                    order.OrderType,
                    order.OrderStatus,
                    OrderStatus.Cancelled))
            {
                continue;
            }

            // Thực hiện logic hủy đơn hàng (Hoàn tiền nếu đã thanh toán, cập nhật lịch sử trạng thái)
            var orderTransitions = await OrderWorkflowMutations.CancelOrderAsync(
                _unitOfWork,
                order,
                staffUserId,
                "Order cancelled automatically because prescription was rejected.",
                cancellationToken);

            // Ghi nhận sự thay đổi tồn kho để gửi thông báo cho khách hàng sau đó
            foreach (var transition in orderTransitions)
            {
                if (mergedTransitions.TryGetValue(transition.VariantId, out var existingTransition))
                {
                    mergedTransitions[transition.VariantId] = existingTransition with
                    {
                        CurrentQuantity = transition.CurrentQuantity
                    };
                }
                else
                {
                    mergedTransitions[transition.VariantId] = transition;
                }
            }
        }

        return mergedTransitions.Values.ToArray();
    }

    private async Task NotifyBackInStockTransitionsAsync(
        IEnumerable<OrderWorkflowMutations.InventoryQuantityTransition> transitions,
        string source,
        CancellationToken cancellationToken)
    {
        foreach (var transition in transitions)
        {
            await _backInStockNotificationService.HandleStockChangeAsync(
                transition.VariantId,
                transition.PreviousQuantity,
                transition.CurrentQuantity,
                source,
                cancellationToken);
        }
    }

    private static void ValidatePrescriptionStatusTransition(PrescriptionStatus currentStatus, PrescriptionStatus nextStatus)
    {
        if (!OrderWorkflowPolicies.CanTransitionPrescriptionStatus(currentStatus, nextStatus))
        {
            throw CreateApiException(HttpStatusCode.BadRequest, "INVALID_PRESCRIPTION_STATUS", "Invalid prescription status update");
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
}
