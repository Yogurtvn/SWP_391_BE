using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.Product;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Product.Request;
using ServiceLayer.DTOs.Product.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/products")]
[ApiController]
public class ProductsController(IProductService productService) : ApiControllerBase
{
    private readonly IProductService _productService = productService;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemResponse>>> GetProducts(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.GetProductsAsync(
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
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductDetailResponse>> GetProduct(int productId, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductByIdAsync(
            productId,
            includeInactive: CanAccessNonPublicCatalogData(),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { errorCode = "PRODUCT_NOT_FOUND", message = "Product not found" });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductIdResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.CreateProductAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{productId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdateProduct(
        int productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.UpdateProductAsync(productId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{productId:int}/status")]
    public async Task<ActionResult<MessageResponse>> UpdateProductStatus(
        int productId,
        [FromBody] UpdateProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.UpdateProductStatusAsync(productId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{productId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteProduct(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(productId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
