using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductImage.Request;
using ServiceLayer.DTOs.ProductImage.Response;

namespace ServiceLayer.Contracts.ProductImage;

public interface IProductImageService
{
    Task<ProductImagesResponse> GetImagesAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ProductImagesResponse> UploadImagesAsync(
        int productId,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateImageMetadataAsync(
        int productId,
        int imageId,
        UpdateProductImageMetadataRequest request,
        CancellationToken cancellationToken = default);

    Task<DeleteProductImageResult> DeleteImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default);
}
