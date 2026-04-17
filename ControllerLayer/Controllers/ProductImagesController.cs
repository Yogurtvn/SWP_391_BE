using ControllerLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.ProductImage;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductImage.Request;
using ServiceLayer.DTOs.ProductImage.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/products/{productId:int}/images")]
[ApiController]
public class ProductImagesController(
    IProductImageService productImageService,
    IWebHostEnvironment environment) : ApiControllerBase
{
    private readonly IProductImageService _productImageService = productImageService;
    private readonly IWebHostEnvironment _environment = environment;

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

        var savedImageUrls = new List<string>();

        try
        {
            foreach (var file in request.Files)
            {
                if (file.Length <= 0)
                {
                    DeleteSavedFiles(savedImageUrls);
                    return BadRequest(new
                    {
                        errorCode = "VALIDATION_ERROR",
                        message = "Invalid product image data",
                        details = new { field = "files", issue = "Uploaded files must not be empty" }
                    });
                }

                if (!string.IsNullOrWhiteSpace(file.ContentType) && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteSavedFiles(savedImageUrls);
                    return BadRequest(new
                    {
                        errorCode = "VALIDATION_ERROR",
                        message = "Invalid product image data",
                        details = new { field = "files", issue = "Uploaded files must be image content" }
                    });
                }

                savedImageUrls.Add(await SaveFileAsync(productId, file, cancellationToken));
            }

            // TODO: replace local-disk storage once the final storage strategy is defined in API_SPEC.md.
            var result = await _productImageService.UploadImagesAsync(productId, savedImageUrls, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            DeleteSavedFiles(savedImageUrls);
            return ApiError(exception);
        }
        catch
        {
            DeleteSavedFiles(savedImageUrls);
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
            DeleteFile(result.ImageUrl);
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

    private async Task<string> SaveFileAsync(int productId, IFormFile file, CancellationToken cancellationToken)
    {
        var uploadsDirectory = GetProductUploadsDirectory(productId);
        Directory.CreateDirectory(uploadsDirectory);

        var originalExtension = Path.GetExtension(file.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(originalExtension) ? ".bin" : originalExtension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var physicalPath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/products/{productId}/{fileName}";
    }

    private void DeleteSavedFiles(IEnumerable<string> imageUrls)
    {
        foreach (var imageUrl in imageUrls)
        {
            DeleteFile(imageUrl);
        }
    }

    private void DeleteFile(string imageUrl)
    {
        var physicalPath = Path.Combine(GetUploadsRootPath(), imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private string GetProductUploadsDirectory(int productId)
    {
        return Path.Combine(GetUploadsRootPath(), "uploads", "products", productId.ToString());
    }

    private string GetUploadsRootPath()
    {
        return _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
    }
}
