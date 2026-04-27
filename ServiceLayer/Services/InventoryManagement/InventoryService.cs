using RepositoryLayer.Common;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Inventory;
using ServiceLayer.Contracts.Notifications;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;
using ServiceLayer.Exceptions;
using System.Net;
using InventoryEntity = RepositoryLayer.Entities.Inventory;

namespace ServiceLayer.Services.InventoryManagement;

/// <summary>
/// Dịch vụ quản lý kho hàng (Inventory), bao gồm cập nhật số lượng tồn kho và cấu hình đặt trước (Pre-order).
/// </summary>
public class InventoryService(
    IUnitOfWork unitOfWork,
    IPreOrderBackInStockNotificationService backInStockNotificationService) : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPreOrderBackInStockNotificationService _backInStockNotificationService = backInStockNotificationService;

    /// <summary>
    /// Lấy danh sách tồn kho với các bộ lọc và phân trang.
    /// </summary>
    public async Task<PagedResult<InventoryListDtoResponse>> GetInventoriesAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? productId,
        bool? isPreOrderAllowed,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var repository = _unitOfWork.Repository<InventoryEntity>();
        var normalizedSearch = NormalizeText(search);
        var (normalizedSortBy, sortDescending) = NormalizeSort(sortBy, sortOrder);

        var pagedInventories = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: inventory =>
                (!variantId.HasValue || inventory.VariantId == variantId.Value) &&
                (!productId.HasValue || inventory.Variant.ProductId == productId.Value) &&
                (!isPreOrderAllowed.HasValue || inventory.IsPreOrderAllowed == isPreOrderAllowed.Value) &&
                (normalizedSearch == null
                    || inventory.Variant.Sku.Contains(normalizedSearch)
                    || (inventory.Variant.Color != null && inventory.Variant.Color.Contains(normalizedSearch))
                    || (inventory.Variant.Size != null && inventory.Variant.Size.Contains(normalizedSearch))
                    || (inventory.Variant.FrameType != null && inventory.Variant.FrameType.Contains(normalizedSearch))
                    || inventory.Variant.Product.ProductName.Contains(normalizedSearch)),
            orderBy: query => ApplyOrdering(query, normalizedSortBy, sortDescending),
            includeProperties: "Variant.Product",
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedInventories.Items
            .Select(MapToListDto)
            .ToList();

        return PagedResult<InventoryListDtoResponse>.Create(
            items,
            pagedInventories.Page,
            pagedInventories.PageSize,
            pagedInventories.TotalItems);
    }

    public async Task<InventoryDtoResponse?> GetInventoryByVariantIdAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        return inventory is null ? null : MapToDto(inventory);
    }

    /// <summary>
    /// Cập nhật thông tin kho hàng cho một biến thể sản phẩm cụ thể.
    /// </summary>
    public async Task<bool> UpdateInventoryAsync(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        if (inventory is null)
        {
            return false;
        }

        var previousQuantity = inventory.Quantity;
        inventory.Quantity = request.Quantity;
        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;
        inventory.ExpectedRestockDate = request.ExpectedRestockDate;
        inventory.PreOrderNote = request.PreOrderNote?.Trim();
        var currentQuantity = inventory.Quantity;

        repository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Gửi thông báo cho khách hàng nếu sản phẩm có hàng trở lại (back in stock)
        await _backInStockNotificationService.HandleStockChangeAsync(
            variantId,
            previousQuantity,
            currentQuantity,
            source: "inventory:update",
            cancellationToken);

        return true;
    }

    public async Task<bool> UpdatePreOrderAsync(int variantId, UpdatePreOrderRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<InventoryEntity>();
        var inventory = await repository.GetByIdAsync(variantId);

        if (inventory is null)
        {
            return false;
        }

        inventory.IsPreOrderAllowed = request.IsPreOrderAllowed;
        inventory.ExpectedRestockDate = request.ExpectedRestockDate;
        inventory.PreOrderNote = request.PreOrderNote?.Trim();

        repository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static InventoryDtoResponse MapToDto(InventoryEntity inventory)
    {
        return new InventoryDtoResponse
        {
            VariantId = inventory.VariantId,
            Quantity = inventory.Quantity,
            IsReadyAvailable = inventory.Quantity > 0,
            IsPreOrderAllowed = inventory.IsPreOrderAllowed,
            ExpectedRestockDate = inventory.ExpectedRestockDate,
            PreOrderNote = inventory.PreOrderNote
        };
    }

    private static InventoryListDtoResponse MapToListDto(InventoryEntity inventory)
    {
        return new InventoryListDtoResponse
        {
            VariantId = inventory.VariantId,
            Quantity = inventory.Quantity,
            IsReadyAvailable = inventory.Quantity > 0,
            IsPreOrderAllowed = inventory.IsPreOrderAllowed,
            ExpectedRestockDate = inventory.ExpectedRestockDate,
            PreOrderNote = inventory.PreOrderNote
        };
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static (string SortBy, bool SortDescending) NormalizeSort(string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = NormalizeText(sortBy)?.ToLowerInvariant() ?? "variantid";
        var normalizedSortOrder = NormalizeText(sortOrder)?.ToLowerInvariant();

        if (normalizedSortBy is not ("variantid" or "quantity" or "expectedrestockdate"))
        {
            throw CreateInvalidQueryException("sortBy", "sortBy is invalid");
        }

        return normalizedSortOrder switch
        {
            null => (normalizedSortBy, false),
            "asc" => (normalizedSortBy, false),
            "desc" => (normalizedSortBy, true),
            _ => throw CreateInvalidQueryException("sortOrder", "sortOrder must be 'asc' or 'desc'")
        };
    }

    private static IOrderedQueryable<InventoryEntity> ApplyOrdering(
        IQueryable<InventoryEntity> query,
        string sortBy,
        bool sortDescending)
    {
        return sortBy switch
        {
            "quantity" when sortDescending => query.OrderByDescending(inventory => inventory.Quantity)
                .ThenByDescending(inventory => inventory.VariantId),
            "quantity" => query.OrderBy(inventory => inventory.Quantity)
                .ThenBy(inventory => inventory.VariantId),
            "expectedrestockdate" when sortDescending => query.OrderByDescending(inventory => inventory.ExpectedRestockDate)
                .ThenByDescending(inventory => inventory.VariantId),
            "expectedrestockdate" => query.OrderBy(inventory => inventory.ExpectedRestockDate)
                .ThenBy(inventory => inventory.VariantId),
            _ when sortDescending => query.OrderByDescending(inventory => inventory.VariantId),
            _ => query.OrderBy(inventory => inventory.VariantId)
        };
    }

    private static ApiException CreateInvalidQueryException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "INVALID_QUERY",
            "Invalid inventory query",
            new { field, issue });
    }
}
