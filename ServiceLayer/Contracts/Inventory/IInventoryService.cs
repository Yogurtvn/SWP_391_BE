using RepositoryLayer.Common;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;

namespace ServiceLayer.Contracts.Inventory;

public interface IInventoryService
{
    Task<PagedResult<InventoryListDtoResponse>> GetInventoriesAsync(
        PaginationRequest paginationRequest,
        int? variantId,
        int? productId,
        bool? isPreOrderAllowed,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task<InventoryDtoResponse?> GetInventoryByVariantIdAsync(int variantId, CancellationToken cancellationToken = default);

    Task<bool> UpdateInventoryAsync(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdatePreOrderAsync(int variantId, UpdatePreOrderRequest request, CancellationToken cancellationToken = default);
}
