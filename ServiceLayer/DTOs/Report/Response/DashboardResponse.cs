namespace ServiceLayer.DTOs.Report.Response;

// DTO trả về dashboard tổng hợp nhanh cho admin (dùng cho GET /api/reports/dashboard)
public class DashboardResponse
{
    public int TotalOrders { get; set; }              // Tổng số đơn hàng trong khoảng thời gian

    public decimal Revenue { get; set; }              // Tổng doanh thu (từ các đơn Completed)

    public int PendingOrders { get; set; }            // Số đơn hàng đang chờ xử lý (Pending)

    public int LowStockVariants { get; set; }         // Số variant có tồn kho thấp (quantity <= ngưỡng)
}
