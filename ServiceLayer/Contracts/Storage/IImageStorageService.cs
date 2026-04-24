using ServiceLayer.DTOs.Common;

namespace ServiceLayer.Contracts.Storage;

public interface IImageStorageService
{
    Task<ImageStorageUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default);

    Task DeleteByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);

    Task DeleteByUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
}
