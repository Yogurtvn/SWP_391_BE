namespace ServiceLayer.DTOs.Report.Response;

// DTO trả về thống kê đơn prescription (dùng cho GET /api/reports/prescriptions-summary)
public class PrescriptionsSummaryResponse
{
    public int TotalPrescriptionOrders { get; set; }  // Tổng số đơn prescription trong khoảng thời gian

    public int Approved { get; set; }                 // Số đơn đã được duyệt (Approved)

    public int NeedMoreInfo { get; set; }             // Deprecated field, always 0 in current runtime flow

    public int Rejected { get; set; }                 // Số đơn bị từ chối (Rejected)
}
