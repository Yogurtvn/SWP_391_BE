namespace ServiceLayer.DTOs.StockReceipt.Response;

// DTO trả về thông tin stock receipt trong danh sách (dùng cho GET /api/stock-receipts với phân trang)
// Có thêm receivedDate so với StockReceiptDtoResponse, nhưng không có Note
public class StockReceiptListDtoResponse
{
    public int ReceiptId { get; set; }              // ID của phiếu nhập hàng

    public int VariantId { get; set; }              // ID của product variant

    public int QuantityReceived { get; set; }       // Số lượng hàng đã nhập

    public DateTime ReceivedDate { get; set; }      // Ngày nhận hàng

    public int? RecordedByUserId { get; set; }      // UserId người nhập hàng (DB vẫn lưu ở StaffId)

    public string? RecordedByName { get; set; }     // Họ tên người nhập hàng

    public string? RecordedByRole { get; set; }     // Vai trò thực tế người nhập hàng (Admin/Staff)
}
