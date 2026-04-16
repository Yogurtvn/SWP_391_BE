using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Report;
using ServiceLayer.DTOs.Report.Response;

namespace ControllerLayer.Controllers;

[Route("api/reports")]   // Route mặc định: /api/reports
[ApiController]           // Tự động validate model state và trả 400 nếu invalid
public class ReportsController(IReportService reportService) : ControllerBase
{
    private readonly IReportService _reportService = reportService; // Inject service qua primary constructor

    // GET /api/reports/orders-summary?startDate=2026-01-01&endDate=2026-12-31
    // Tổng số đơn, số đơn theo loại trong khoảng thời gian
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpGet("orders-summary")]
    public async Task<ActionResult<OrdersSummaryResponse>> GetOrdersSummary(
        [FromQuery] DateTime? startDate = null,            // Ngày bắt đầu (optional)
        [FromQuery] DateTime? endDate = null,              // Ngày kết thúc (optional)
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetOrdersSummaryAsync(startDate, endDate, cancellationToken); // Gọi service thống kê
        return Ok(result);                                 // 200 OK với thống kê đơn hàng
    }

    // GET /api/reports/revenues-summary?startDate=2026-01-01&endDate=2026-12-31&groupBy=month
    // Doanh thu theo thời gian, nhóm theo day/week/month
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpGet("revenues-summary")]
    public async Task<ActionResult<RevenuesSummaryResponse>> GetRevenuesSummary(
        [FromQuery] DateTime? startDate = null,            // Ngày bắt đầu (optional)
        [FromQuery] DateTime? endDate = null,              // Ngày kết thúc (optional)
        [FromQuery] string groupBy = "month",              // Nhóm theo: day, week, month (mặc định = month)
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetRevenuesSummaryAsync(startDate, endDate, groupBy, cancellationToken); // Gọi service thống kê doanh thu
        return Ok(result);                                 // 200 OK với danh sách doanh thu theo nhóm
    }

    // GET /api/reports/prescriptions-summary?startDate=2026-01-01&endDate=2026-12-31
    // Thống kê đơn prescription theo trạng thái
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpGet("prescriptions-summary")]
    public async Task<ActionResult<PrescriptionsSummaryResponse>> GetPrescriptionsSummary(
        [FromQuery] DateTime? startDate = null,            // Ngày bắt đầu (optional)
        [FromQuery] DateTime? endDate = null,              // Ngày kết thúc (optional)
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetPrescriptionsSummaryAsync(startDate, endDate, cancellationToken); // Gọi service thống kê prescription
        return Ok(result);                                 // 200 OK với thống kê prescription
    }

    // GET /api/reports/pre-orders-summary?startDate=2026-01-01&endDate=2026-12-31
    // Thống kê đơn pre-order theo trạng thái
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpGet("pre-orders-summary")]
    public async Task<ActionResult<PreOrdersSummaryResponse>> GetPreOrdersSummary(
        [FromQuery] DateTime? startDate = null,            // Ngày bắt đầu (optional)
        [FromQuery] DateTime? endDate = null,              // Ngày kết thúc (optional)
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetPreOrdersSummaryAsync(startDate, endDate, cancellationToken); // Gọi service thống kê pre-order
        return Ok(result);                                 // 200 OK với thống kê pre-order
    }

    // GET /api/reports/dashboard?startDate=2026-01-01&endDate=2026-12-31
    // Dashboard tổng hợp nhanh cho admin
    [AllowAnonymous]  // Tạm thời cho phép tất cả (sau này sẽ đổi thành [Authorize(Roles = "Admin")])
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        [FromQuery] DateTime? startDate = null,            // Ngày bắt đầu (optional)
        [FromQuery] DateTime? endDate = null,              // Ngày kết thúc (optional)
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDashboardAsync(startDate, endDate, cancellationToken); // Gọi service tổng hợp dashboard
        return Ok(result);                                 // 200 OK với dashboard tổng hợp
    }
}
