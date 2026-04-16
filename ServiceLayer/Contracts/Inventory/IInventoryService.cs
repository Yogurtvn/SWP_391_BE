using RepositoryLayer.Common;
using ServiceLayer.DTOs.Inventory.Request;
using ServiceLayer.DTOs.Inventory.Response;

namespace ServiceLayer.Contracts.Inventory;

// Interface định nghĩa các nghiệp vụ quản lý Inventory (tồn kho)
public interface IInventoryService
{
    // Lấy danh sách inventory có phân trang, lọc theo variantId/productId/isPreOrderAllowed và tìm kiếm
    Task<PagedResult<InventoryListDtoResponse>> GetInventoriesAsync(
        PaginationRequest paginationRequest,
        int? variantId,                                   // Lọc theo variant cụ thể (optional)
        int? productId,                                   // Lọc theo product (optional)
        bool? isPreOrderAllowed,                          // Lọc theo trạng thái pre-order (optional)
        string? search,                                   // Tìm kiếm (optional)
        CancellationToken cancellationToken = default);

    // Lấy chi tiết inventory của 1 variant, trả về null nếu không tìm thấy
    Task<InventoryDtoResponse?> GetInventoryByVariantIdAsync(int variantId, CancellationToken cancellationToken = default);

    // Admin/Staff cập nhật tồn kho trực tiếp, trả về true nếu thành công, false nếu không tìm thấy
    Task<bool> UpdateInventoryAsync(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default);

    // Bật/tắt cài đặt pre-order riêng, trả về true nếu thành công, false nếu không tìm thấy
    Task<bool> UpdatePreOrderAsync(int variantId, UpdatePreOrderRequest request, CancellationToken cancellationToken = default);
}
