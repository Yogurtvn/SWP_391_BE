namespace ServiceLayer.DTOs.Report.Response;

// DTO trả về tổng số đơn và phân loại theo OrderType (dùng cho GET /api/reports/orders-summary)
public class OrdersSummaryResponse
{
    public int TotalOrders { get; set; }              // Tổng số đơn hàng trong khoảng thời gian

    public int ReadyOrders { get; set; }              // Số đơn hàng loại Ready (mua ngay)

    public int PreOrderOrders { get; set; }           // Số đơn hàng loại PreOrder (đặt trước)

    public int PrescriptionOrders { get; set; }       // Số đơn hàng loại Prescription (theo đơn)
}
