using ControllerLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.DTOs.Cart.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

[Route("api/prescription-images")]
[ApiController]
public class PrescriptionImagesController(IWebHostEnvironment environment) : ApiControllerBase
{
    private readonly IWebHostEnvironment _environment = environment;

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

        string? savedFileUrl = null;

        try
        {
            // TODO: apply a file size limit after a shared project rule is defined in API_SPEC.md.
            var result = await SaveFileAsync(request.File, cancellationToken);
            savedFileUrl = result.FileUrl;
            return Ok(result);
        }
        catch (ApiException exception)
        {
            if (savedFileUrl is not null)
            {
                DeleteFile(savedFileUrl);
            }

            return ApiError(exception);
        }
        catch
        {
            if (savedFileUrl is not null)
            {
                DeleteFile(savedFileUrl);
            }

            throw;
        }
    }

    private async Task<PrescriptionImageUploadResponse> SaveFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var uploadsDirectory = GetPrescriptionUploadsDirectory();
        Directory.CreateDirectory(uploadsDirectory);

        var extension = ResolveFileExtension(file);
        var fileName = $"prescription-{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsDirectory, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return new PrescriptionImageUploadResponse
        {
            FileName = fileName,
            FileUrl = $"/uploads/prescriptions/{fileName}"
        };
    }

    private void DeleteFile(string fileUrl)
    {
        var physicalPath = Path.Combine(GetUploadsRootPath(), fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private string GetPrescriptionUploadsDirectory()
    {
        return Path.Combine(GetUploadsRootPath(), "uploads", "prescriptions");
    }

    private string GetUploadsRootPath()
    {
        return _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
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
}
