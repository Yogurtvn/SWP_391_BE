namespace ServiceLayer.DTOs.Report.Response;

// DTO trả về thống kê đơn pre-order (dùng cho GET /api/reports/pre-orders-summary)
public class PreOrdersSummaryResponse
{
    public int TotalPreOrders { get; set; }           // Tổng số đơn pre-order trong khoảng thời gian

    public int AwaitingStock { get; set; }            // Số đơn đang chờ hàng (AwaitingStock)

    public int Processing { get; set; }               // Số đơn đang xử lý (Processing)

    public int Completed { get; set; }                // Số đơn đã hoàn thành (Completed)
}
