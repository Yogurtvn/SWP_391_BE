using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Catalog;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Promotions;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[ApiController]
[Route("api/admin/promotions")]
[Authorize(Roles = "Admin")]
public class AdminPromotionController(IPromotionService promotionService) : ApiControllerBase
{
    private readonly IPromotionService _promotionService = promotionService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PromotionResponse>>> GetPromotions(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.GetPromotionsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromotionResponse>> GetPromotionById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.GetPromotionByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpPost]
    public async Task<ActionResult<PromotionResponse>> CreatePromotion(
        [FromBody] CreatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.CreatePromotionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPromotionById), new { id = result.PromotionId }, result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<PromotionResponse>> UpdatePromotion(
        int id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.UpdatePromotionAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<MessageResponse>> UpdatePromotionStatus(
        int id,
        [FromBody] UpdatePromotionStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.UpdatePromotionStatusAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePromotion(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _promotionService.DeletePromotionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpPost("{promotionId:int}/variants")]
    public async Task<ActionResult<MessageResponse>> AssignPromotionToVariants(
        int promotionId,
        [FromBody] AssignPromotionVariantsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _promotionService.AssignPromotionToVariantsAsync(promotionId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpPost("{promotionId:int}/variants/{variantId:int}")]
    public async Task<ActionResult> AssignPromotionToVariant(
        int promotionId,
        int variantId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _promotionService.AssignPromotionToVariantAsync(promotionId, variantId, cancellationToken);
            return NoContent();
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [HttpDelete("variants/{variantId:int}")]
    public async Task<ActionResult> RemovePromotionFromVariant(
        int variantId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _promotionService.RemovePromotionFromVariantAsync(variantId, cancellationToken);
            return NoContent();
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
