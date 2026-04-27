using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.DTOs.Prescription.Request;
using ServiceLayer.DTOs.Prescription.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller xử lý các yêu cầu HTTP liên quan đến việc quản lý và duyệt đơn thuốc.
/// </summary>
[Route("api/prescriptions")]
[ApiController]
public class PrescriptionsController(IPrescriptionService prescriptionService) : ApiControllerBase
{
    private readonly IPrescriptionService _prescriptionService = prescriptionService;

    [Authorize(Roles = "Staff,Admin")]
    [HttpGet]
    /// <summary>
    /// API Lấy danh sách các đơn thuốc (Dành cho Admin/Staff).
    /// </summary>
    public async Task<ActionResult> GetPrescriptions(
        [FromQuery] GetPrescriptionsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _prescriptionService.GetPrescriptionsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpGet("{prescriptionId:int}")]
    public async Task<ActionResult<PrescriptionDetailResponse>> GetPrescriptionById(
        int prescriptionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _prescriptionService.GetPrescriptionByIdAsync(prescriptionId, cancellationToken);

            if (result is null)
            {
                return NotFound(new { errorCode = "PRESCRIPTION_NOT_FOUND", message = "Prescription not found" });
            }

            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPatch("{prescriptionId:int}/review")]
    /// <summary>
    /// API Phê duyệt hoặc từ chối một đơn thuốc.
    /// </summary>
    public async Task<ActionResult<PrescriptionStatusResponse>> ReviewPrescription(
        int prescriptionId,
        [FromBody] ReviewPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _prescriptionService.ReviewPrescriptionAsync(userId, prescriptionId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Staff,Admin")]
    [Obsolete("Deprecated. Use PATCH /api/prescriptions/{id}/review with reviewing/approved/rejected.")]
    [HttpPatch("{prescriptionId:int}/request-more-info")]
    public async Task<ActionResult<PrescriptionStatusResponse>> RequestMoreInfo(
        int prescriptionId,
        [FromBody] RequestMorePrescriptionInfoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _prescriptionService.RequestMoreInfoAsync(userId, prescriptionId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Customer")]
    [Obsolete("Deprecated. Resubmit flow is no longer supported.")]
    [HttpPatch("{prescriptionId:int}/resubmit")]
    public async Task<ActionResult<PrescriptionStatusResponse>> ResubmitPrescription(
        int prescriptionId,
        [FromBody] ResubmitPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _prescriptionService.ResubmitPrescriptionAsync(userId, prescriptionId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
