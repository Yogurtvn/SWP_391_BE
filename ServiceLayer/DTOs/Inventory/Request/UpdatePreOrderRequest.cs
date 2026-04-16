namespace ServiceLayer.DTOs.Inventory.Request;

// DTO nhận dữ liệu từ client khi bật/tắt pre-order (dùng cho PATCH /api/inventories/{variantId}/pre-orders)
// Chỉ cập nhật các field liên quan đến pre-order, không đụng đến quantity
public class UpdatePreOrderRequest
{
    public bool IsPreOrderAllowed { get; set; }           // Bật/tắt cho phép đặt hàng trước

    public DateTime? ExpectedRestockDate { get; set; }    // Ngày dự kiến nhập hàng lại (nullable, optional)

    public string? PreOrderNote { get; set; }             // Ghi chú pre-order (nullable, optional)
}
