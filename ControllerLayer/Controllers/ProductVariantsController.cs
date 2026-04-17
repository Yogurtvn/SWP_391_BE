using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.ProductVariant;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductVariant.Request;
using ServiceLayer.DTOs.ProductVariant.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api")]
[ApiController]
public class ProductVariantsController(IProductVariantService productVariantService) : ApiControllerBase
{
    private readonly IProductVariantService _productVariantService = productVariantService;

    [AllowAnonymous]
    [HttpGet("products/{productId:int}/variants")]
    public async Task<ActionResult<PagedResult<ProductVariantListItemResponse>>> GetVariantsByProduct(
        int productId,
        [FromQuery] GetProductVariantsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productVariantService.GetVariantsByProductAsync(
                productId,
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
    [HttpGet("variants/{variantId:int}")]
    public async Task<ActionResult<ProductVariantDetailResponse>> GetVariant(
        int variantId,
        CancellationToken cancellationToken)
    {
        var result = await _productVariantService.GetVariantByIdAsync(
            variantId,
            includeInactive: CanAccessNonPublicCatalogData(),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { errorCode = "VARIANT_NOT_FOUND", message = "Variant not found" });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("products/{productId:int}/variants")]
    public async Task<ActionResult<ProductVariantIdResponse>> CreateVariant(
        int productId,
        [FromBody] CreateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productVariantService.CreateVariantAsync(productId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("variants/{variantId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdateVariant(
        int variantId,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productVariantService.UpdateVariantAsync(variantId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("variants/{variantId:int}/status")]
    public async Task<ActionResult<MessageResponse>> UpdateVariantStatus(
        int variantId,
        [FromBody] UpdateProductVariantStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productVariantService.UpdateVariantStatusAsync(variantId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("variants/{variantId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteVariant(int variantId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productVariantService.DeleteVariantAsync(variantId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
