using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.StockReceipt.Request;

// DTO nhận dữ liệu từ client khi ghi nhận nhập thêm hàng (dùng cho POST /api/stock-receipts)
public class CreateStockReceiptRequest
{
    [Required(ErrorMessage = "VariantId is required.")]               // Bắt buộc phải có VariantId
    public int VariantId { get; set; }                                // ID của product variant cần nhập hàng

    [Range(1, int.MaxValue, ErrorMessage = "QuantityReceived must be greater than 0.")] // Số lượng phải > 0
    public int QuantityReceived { get; set; }                         // Số lượng hàng nhập vào

    public string? Note { get; set; }                                 // Ghi chú nhập hàng (nullable, optional)
}
