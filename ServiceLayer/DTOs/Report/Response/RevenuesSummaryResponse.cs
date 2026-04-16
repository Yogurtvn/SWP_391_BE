namespace ServiceLayer.DTOs.Report.Response;

// DTO trả về doanh thu trong 1 khoảng thời gian (dùng cho GET /api/reports/revenues-summary)
// Mỗi item đại diện cho 1 nhóm thời gian (ngày/tuần/tháng)
public class RevenuesSummaryResponse
{
    public List<RevenueGroupItem> Items { get; set; } = []; // Danh sách doanh thu theo từng nhóm thời gian
}

// DTO con đại diện cho doanh thu của 1 nhóm thời gian
public class RevenueGroupItem
{
    public string Period { get; set; } = string.Empty;  // Tên khoảng thời gian (vd: "2026-04-16", "2026-04", "2026-W16")

    public decimal Revenue { get; set; }                // Tổng doanh thu trong khoảng thời gian đó

    public int OrderCount { get; set; }                 // Số đơn hàng trong khoảng thời gian đó
}
