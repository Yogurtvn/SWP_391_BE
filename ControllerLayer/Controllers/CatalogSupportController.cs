using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.CatalogSupport;
using ServiceLayer.DTOs.CatalogSupport.Request;
using ServiceLayer.DTOs.CatalogSupport.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api")]
[ApiController]
public class CatalogSupportController(ICatalogSupportService catalogSupportService) : ApiControllerBase
{
    private readonly ICatalogSupportService _catalogSupportService = catalogSupportService;

    [AllowAnonymous]
    [HttpGet("products/{productId:int}/prescription-eligibility")]
    public async Task<ActionResult<PrescriptionEligibilityResponse>> GetPrescriptionEligibility(
        int productId,
        CancellationToken cancellationToken)
    {
        var result = await _catalogSupportService.GetPrescriptionEligibilityAsync(productId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { errorCode = "PRODUCT_NOT_FOUND", message = "Product not found" });
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("variants/{variantId:int}/availability")]
    public async Task<ActionResult<VariantAvailabilityResponse>> GetVariantAvailability(
        int variantId,
        CancellationToken cancellationToken)
    {
        var result = await _catalogSupportService.GetVariantAvailabilityAsync(variantId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { errorCode = "VARIANT_NOT_FOUND", message = "Variant not found" });
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("prescription-pricings/calculate")]
    public async Task<ActionResult<PrescriptionPricingResponse>> CalculatePrescriptionPricing(
        [FromBody] CalculatePrescriptionPricingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _catalogSupportService.CalculatePrescriptionPricingAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
