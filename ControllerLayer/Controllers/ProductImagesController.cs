using ControllerLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.ProductImage;
using ServiceLayer.Contracts.Storage;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductImage.Request;
using ServiceLayer.DTOs.ProductImage.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/products/{productId:int}/images")]
[ApiController]
public class ProductImagesController(
    IProductImageService productImageService,
    IImageStorageService imageStorageService) : ApiControllerBase
{
    private readonly IProductImageService _productImageService = productImageService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ProductImagesResponse>> GetImages(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productImageService.GetImagesAsync(
                productId,
                includeInactive: CanAccessNonPublicCatalogData(),
                cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductImagesResponse>> UploadImages(
        int productId,
        [FromForm] UploadProductImagesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return BadRequest(new
            {
                errorCode = "VALIDATION_ERROR",
                message = "Invalid product image data",
                details = new { field = "files", issue = "At least one file is required" }
            });
        }

        var uploadedImages = new List<UploadedImage>();

        try
        {
            foreach (var file in request.Files)
            {
                if (file.Length <= 0)
                {
                    await DeleteSavedFilesAsync(uploadedImages);
                    return BadRequest(new
                    {
                        errorCode = "VALIDATION_ERROR",
                        message = "Invalid product image data",
                        details = new { field = "files", issue = "Uploaded files must not be empty" }
                    });
                }

                if (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    await DeleteSavedFilesAsync(uploadedImages);
                    return BadRequest(new
                    {
                        errorCode = "VALIDATION_ERROR",
                        message = "Invalid product image data",
                        details = new { field = "files", issue = "Uploaded files must be image content" }
                    });
                }

                uploadedImages.Add(await SaveFileAsync(productId, file, cancellationToken));
            }

            var result = await _productImageService.UploadImagesAsync(
                productId,
                uploadedImages.Select(image => image.Url).ToList(),
                cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            await DeleteSavedFilesAsync(uploadedImages);
            return ApiError(exception);
        }
        catch
        {
            await DeleteSavedFilesAsync(uploadedImages);
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{imageId:int}")]
    public async Task<ActionResult<MessageResponse>> UpdateImageMetadata(
        int productId,
        int imageId,
        [FromBody] UpdateProductImageMetadataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productImageService.UpdateImageMetadataAsync(productId, imageId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{imageId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteImage(
        int productId,
        int imageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productImageService.DeleteImageAsync(productId, imageId, cancellationToken);
            await DeleteFileAsync(result.ImageUrl, cancellationToken);
            return Ok(new MessageResponse
            {
                Message = result.Message
            });
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    private async Task<UploadedImage> SaveFileAsync(int productId, IFormFile file, CancellationToken cancellationToken)
    {
        var originalExtension = Path.GetExtension(file.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(originalExtension) ? ".bin" : originalExtension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";

        await using var stream = file.OpenReadStream();
        var uploadResult = await _imageStorageService.UploadImageAsync(
            stream,
            fileName,
            GetProductFolder(productId),
            cancellationToken);

        return new UploadedImage(uploadResult.Url, uploadResult.PublicId);
    }

    private async Task DeleteSavedFilesAsync(IEnumerable<UploadedImage> uploadedImages)
    {
        foreach (var uploadedImage in uploadedImages)
        {
            try
            {
                await _imageStorageService.DeleteByPublicIdAsync(uploadedImage.PublicId, CancellationToken.None);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private Task DeleteFileAsync(string imageUrl, CancellationToken cancellationToken)
    {
        return _imageStorageService.DeleteByUrlAsync(imageUrl, cancellationToken);
    }

    private static string GetProductFolder(int productId)
    {
        return $"products/{productId}";
    }

    private sealed record UploadedImage(string Url, string PublicId);
}
