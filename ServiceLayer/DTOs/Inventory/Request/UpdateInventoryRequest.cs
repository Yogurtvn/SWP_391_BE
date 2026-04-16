using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Inventory.Request;

// DTO nhận dữ liệu từ client khi cập nhật inventory (dùng cho PUT /api/inventories/{variantId})
// Admin/Staff cập nhật tồn kho trực tiếp
public class UpdateInventoryRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")] // Số lượng không được âm
    public int Quantity { get; set; }                     // Số lượng tồn kho mới

    public bool IsPreOrderAllowed { get; set; }           // Bật/tắt cho phép đặt hàng trước

    public DateTime? ExpectedRestockDate { get; set; }    // Ngày dự kiến nhập hàng (nullable, optional)

    public string? PreOrderNote { get; set; }             // Ghi chú về đặt hàng trước (nullable, optional)
}
