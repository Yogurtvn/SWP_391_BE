namespace ServiceLayer.DTOs.Inventory.Response;

// DTO trả về thông tin chi tiết inventory của 1 variant (dùng cho GET /api/inventories/{variantId})
public class InventoryDtoResponse
{
    public int VariantId { get; set; }          // ID của product variant

    public int Quantity { get; set; }           // Số lượng tồn kho hiện tại

    public bool IsPreOrderAllowed { get; set; } // Có cho phép đặt hàng trước không
}
