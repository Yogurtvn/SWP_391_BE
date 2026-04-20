using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Catalog;
using ServiceLayer.DTOs.Promotions;

namespace ControllerLayer.Controllers.Admin;

[ApiController]
[Route("api/admin/promotions")]
[Authorize(Roles = "Admin")]
public class AdminPromotionController(IPromotionService promotionService) : ControllerBase
{
    private readonly IPromotionService _promotionService = promotionService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PromotionResponse>>> GetPromotions(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promotionService.GetPromotionsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromotionResponse>> GetPromotionById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _promotionService.GetPromotionByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PromotionResponse>> CreatePromotion(
        [FromBody] CreatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promotionService.CreatePromotionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPromotionById), new { id = result.PromotionId }, result);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<PromotionResponse>> UpdatePromotion(
        int id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promotionService.UpdatePromotionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePromotion(
        int id,
        CancellationToken cancellationToken)
    {
        await _promotionService.DeletePromotionAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{promotionId:int}/variants/{variantId:int}")]
    public async Task<ActionResult> AssignPromotionToVariant(
        int promotionId,
        int variantId,
        CancellationToken cancellationToken)
    {
        await _promotionService.AssignPromotionToVariantAsync(promotionId, variantId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("variants/{variantId:int}")]
    public async Task<ActionResult> RemovePromotionFromVariant(
        int variantId,
        CancellationToken cancellationToken)
    {
        await _promotionService.RemovePromotionFromVariantAsync(variantId, cancellationToken);
        return NoContent();
    }
}
