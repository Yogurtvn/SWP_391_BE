using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Report;
using ServiceLayer.DTOs.Report.Response;

namespace ControllerLayer.Controllers;

[Route("api/reports")]
[ApiController]
//[Authorize(Roles = "Admin")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    private readonly IReportService _reportService = reportService;

    [HttpGet("orders-summary")]
    public async Task<ActionResult<OrdersSummaryResponse>> GetOrdersSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetOrdersSummaryAsync(startDate, endDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenues-summary")]
    public async Task<ActionResult<RevenuesSummaryResponse>> GetRevenuesSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string groupBy = "month",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetRevenuesSummaryAsync(startDate, endDate, groupBy, cancellationToken);
        return Ok(result);
    }

    [HttpGet("prescriptions-summary")]
    public async Task<ActionResult<PrescriptionsSummaryResponse>> GetPrescriptionsSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetPrescriptionsSummaryAsync(startDate, endDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pre-orders-summary")]
    public async Task<ActionResult<PreOrdersSummaryResponse>> GetPreOrdersSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetPreOrdersSummaryAsync(startDate, endDate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDashboardAsync(startDate, endDate, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("dashboard-chart")]
    public async Task<ActionResult<DashboardChartResponse>> GetDashboardChart(
        [FromQuery] string timeRange = "year",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDashboardChartAsync(timeRange, cancellationToken);
        return Ok(result);
    }
}
