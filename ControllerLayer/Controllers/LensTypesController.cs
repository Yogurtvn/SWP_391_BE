using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.LensType;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.LensType.Request;
using ServiceLayer.DTOs.LensType.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/lens-types")]
[ApiController]
public class LensTypesController(ILensTypeService lensTypeService) : ApiControllerBase
{
    private readonly ILensTypeService _lensTypeService = lensTypeService;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<LensTypeListItemResponse>>> GetLensTypes(
        [FromQuery] GetLensTypesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lensTypeService.GetLensTypesAsync(
                request,
                includeInactive: CanAccessNonPublicCatalogData(),
                cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [AllowAnonymous]
    [HttpGet("{lensTypeId:int}")]
    public async Task<ActionResult<LensTypeDetailResponse>> GetLensType(int lensTypeId, CancellationToken cancellationToken)
    {
        var result = await _lensTypeService.GetLensTypeByIdAsync(
            lensTypeId,
            includeInactive: CanAccessNonPublicCatalogData(),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { errorCode = "LENS_TYPE_NOT_FOUND", message = "Lens type not found" });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<LensTypeIdResponse>> CreateLensType(
        [FromBody] CreateLensTypeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lensTypeService.CreateLensTypeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{lensTypeId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdateLensType(
        int lensTypeId,
        [FromBody] UpdateLensTypeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lensTypeService.UpdateLensTypeAsync(lensTypeId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{lensTypeId:int}/status")]
    public async Task<ActionResult<MessageResponse>> UpdateLensTypeStatus(
        int lensTypeId,
        [FromBody] UpdateLensTypeStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lensTypeService.UpdateLensTypeStatusAsync(lensTypeId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{lensTypeId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteLensType(int lensTypeId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lensTypeService.DeleteLensTypeAsync(lensTypeId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
