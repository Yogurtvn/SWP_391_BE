using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Storage;
using ServiceLayer.DTOs.Common;

namespace ServiceLayer.Services.Storage;

public class CloudinaryImageStorageService(IOptions<CloudinaryOptions> options) : IImageStorageService
{
    private readonly CloudinaryOptions _options = options.Value;
    private readonly Cloudinary _cloudinary = BuildCloudinaryClient(options.Value);

    public async Task<ImageStorageUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new ArgumentException("Folder is required.", nameof(folder));
        }

        ValidateConfiguration();
        cancellationToken.ThrowIfCancellationRequested();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = NormalizeFolder(folder),
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        cancellationToken.ThrowIfCancellationRequested();

        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        if (uploadResult.SecureUrl is null || string.IsNullOrWhiteSpace(uploadResult.PublicId))
        {
            throw new InvalidOperationException("Cloudinary upload failed: secure URL or public ID was not returned.");
        }

        return new ImageStorageUploadResult
        {
            Url = uploadResult.SecureUrl.AbsoluteUri,
            PublicId = uploadResult.PublicId
        };
    }

    public async Task DeleteByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        ValidateConfiguration();
        cancellationToken.ThrowIfCancellationRequested();

        var deletionResult = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Invalidate = true
        });
        cancellationToken.ThrowIfCancellationRequested();

        if (deletionResult.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {deletionResult.Error.Message}");
        }
    }

    public async Task DeleteByUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (!TryExtractPublicId(imageUrl, out var publicId))
        {
            return;
        }

        await DeleteByPublicIdAsync(publicId, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (IsMissing(_options.CloudName)
            || IsMissing(_options.ApiKey)
            || IsMissing(_options.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing or still using placeholders.");
        }
    }

    private static Cloudinary BuildCloudinaryClient(CloudinaryOptions options)
    {
        var account = new Account(
            options.CloudName?.Trim(),
            options.ApiKey?.Trim(),
            options.ApiSecret?.Trim());

        var cloudinary = new Cloudinary(account);
        cloudinary.Api.Secure = true;
        return cloudinary;
    }

    private static string NormalizeFolder(string folder)
    {
        return folder.Trim().Trim('/');
    }

    private static bool TryExtractPublicId(string imageUrl, out string publicId)
    {
        publicId = string.Empty;

        if (string.IsNullOrWhiteSpace(imageUrl)
            || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            || !uri.Host.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var uploadIndex = Array.FindIndex(segments, segment => segment.Equals("upload", StringComparison.OrdinalIgnoreCase));

        if (uploadIndex < 0 || uploadIndex == segments.Length - 1)
        {
            return false;
        }

        var publicIdSegments = segments[(uploadIndex + 1)..];
        if (publicIdSegments.Length == 0)
        {
            return false;
        }

        if (IsVersionSegment(publicIdSegments[0]))
        {
            publicIdSegments = publicIdSegments[1..];
            if (publicIdSegments.Length == 0)
            {
                return false;
            }
        }

        var lastSegment = publicIdSegments[^1];
        var extension = Path.GetExtension(lastSegment);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            publicIdSegments[^1] = lastSegment[..^extension.Length];
        }

        publicId = string.Join('/', publicIdSegments);
        return !string.IsNullOrWhiteSpace(publicId);
    }

    private static bool IsVersionSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment.Length <= 1 || segment[0] != 'v')
        {
            return false;
        }

        for (var i = 1; i < segment.Length; i++)
        {
            if (!char.IsDigit(segment[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.StartsWith("__SET_", StringComparison.Ordinal)
            || string.Equals(value, "TBD", StringComparison.OrdinalIgnoreCase);
    }
}
