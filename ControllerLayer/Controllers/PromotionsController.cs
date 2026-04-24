using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Catalog;
using ServiceLayer.DTOs.Promotions;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(IPromotionService promotionService) : ApiControllerBase
{
    private readonly IPromotionService _promotionService = promotionService;

    [AllowAnonymous]
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<PromotionResponse>>> GetAvailablePromotions(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _promotionService.GetAvailablePromotionsAsync(limit, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
