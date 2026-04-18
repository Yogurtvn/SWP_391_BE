using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.DTOs.Prescription.Request;
using ServiceLayer.DTOs.Prescription.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Authorize(Roles = "Staff,Admin")]
[Route("api/prescriptions")]
[ApiController]
public class PrescriptionsController(IPrescriptionService prescriptionService) : ApiControllerBase
{
    private readonly IPrescriptionService _prescriptionService = prescriptionService;

    [HttpGet]
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

    [HttpPatch("{prescriptionId:int}/review")]
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
}
