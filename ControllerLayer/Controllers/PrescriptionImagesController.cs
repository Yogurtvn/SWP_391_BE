using ControllerLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Storage;
using ServiceLayer.DTOs.Cart.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/prescription-images")]
[ApiController]
public class PrescriptionImagesController(IImageStorageService imageStorageService) : ApiControllerBase
{
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    [Authorize(Roles = "Customer")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PrescriptionImageUploadResponse>> UploadPrescriptionImage(
        [FromForm] UploadPrescriptionImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(CreateInvalidUploadResponse("file", "file is required"));
        }

        if (request.File.Length <= 0)
        {
            return BadRequest(CreateInvalidUploadResponse("file", "Uploaded file must not be empty"));
        }

        if (string.IsNullOrWhiteSpace(request.File.ContentType)
            || !request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(CreateInvalidUploadResponse("file", "Uploaded file must be image content"));
        }

        string? uploadedPublicId = null;

        try
        {
            var uploadResult = await SaveFileAsync(request.File, cancellationToken);
            uploadedPublicId = uploadResult.PublicId;
            return Ok(uploadResult.Response);
        }
        catch (ApiException exception)
        {
            if (uploadedPublicId is not null)
            {
                await DeleteFileAsync(uploadedPublicId);
            }

            return ApiError(exception);
        }
        catch
        {
            if (uploadedPublicId is not null)
            {
                await DeleteFileAsync(uploadedPublicId);
            }

            throw;
        }
    }

    private async Task<UploadedPrescriptionImage> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = ResolveFileExtension(file);
        var fileName = $"prescription-{Guid.NewGuid():N}{extension}";

        await using var stream = file.OpenReadStream();
        var uploadedImage = await _imageStorageService.UploadImageAsync(
            stream,
            fileName,
            "prescriptions",
            cancellationToken);

        return new UploadedPrescriptionImage(
            new PrescriptionImageUploadResponse
            {
                FileName = fileName,
                FileUrl = uploadedImage.Url
            },
            uploadedImage.PublicId);
    }

    private Task DeleteFileAsync(string publicId)
    {
        return _imageStorageService.DeleteByPublicIdAsync(publicId, CancellationToken.None);
    }

    private static string ResolveFileExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);

        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/tiff" => ".tiff",
            _ => ".img"
        };
    }

    private static object CreateInvalidUploadResponse(string field, string issue)
    {
        return new
        {
            errorCode = "INVALID_FILE_UPLOAD",
            message = "Invalid prescription image upload",
            details = new { field, issue }
        };
    }

    private sealed record UploadedPrescriptionImage(PrescriptionImageUploadResponse Response, string PublicId);
}
