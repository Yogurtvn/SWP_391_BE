using ServiceLayer.DTOs.Report.Response;

namespace ServiceLayer.Contracts.Report;

// Interface định nghĩa các nghiệp vụ báo cáo thống kê (Reports)
public interface IReportService
{
    // Tổng số đơn và phân loại theo loại đơn hàng trong khoảng thời gian
    Task<OrdersSummaryResponse> GetOrdersSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

    // Doanh thu theo thời gian, nhóm theo ngày/tuần/tháng
    Task<RevenuesSummaryResponse> GetRevenuesSummaryAsync(DateTime? startDate, DateTime? endDate, string groupBy = "month", CancellationToken cancellationToken = default);

    // Thống kê đơn prescription theo trạng thái
    Task<PrescriptionsSummaryResponse> GetPrescriptionsSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

    // Thống kê đơn pre-order theo trạng thái
    Task<PreOrdersSummaryResponse> GetPreOrdersSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

    // Dashboard tổng hợp nhanh cho admin
    Task<DashboardResponse> GetDashboardAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

    // Dashboard biểu đồ theo mốc thời gian (1 tuần, 1 tháng, 6 tháng, 1 năm)
    Task<DashboardChartResponse> GetDashboardChartAsync(string timeRange, CancellationToken cancellationToken = default);
}
