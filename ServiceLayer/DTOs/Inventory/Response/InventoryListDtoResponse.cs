namespace ServiceLayer.DTOs.Inventory.Response;

// DTO trả về thông tin inventory trong danh sách (dùng cho GET /api/inventories với phân trang)
// Có thêm field expectedRestockDate so với InventoryDtoResponse
public class InventoryListDtoResponse
{
    public int VariantId { get; set; }                    // ID của product variant

    public int Quantity { get; set; }                     // Số lượng tồn kho hiện tại

    public bool IsPreOrderAllowed { get; set; }           // Có cho phép đặt hàng trước không

    public DateTime? ExpectedRestockDate { get; set; }    // Ngày dự kiến nhập hàng lại (nullable)
}
