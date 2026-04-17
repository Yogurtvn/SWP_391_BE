using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.ProductImage;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductImage.Request;
using ServiceLayer.DTOs.ProductImage.Response;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.ProductImageManagement;

public class ProductImageService(IUnitOfWork unitOfWork) : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ProductImagesResponse> GetImagesAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId, includeInactive);

        var repository = _unitOfWork.Repository<ProductImage>();
        var images = (await repository.FindAsync(
                filter: image => image.ProductId == productId,
                orderBy: query => query.OrderBy(image => image.DisplayOrder).ThenBy(image => image.ImageId),
                tracked: false))
            .Select(MapImage)
            .ToList();

        return new ProductImagesResponse
        {
            Items = images
        };
    }

    public async Task<ProductImagesResponse> UploadImagesAsync(
        int productId,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken = default)
    {
        if (imageUrls.Count == 0)
        {
            throw new ApiException(
                (int)HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "Invalid product image data",
                new { field = "files", issue = "At least one file is required" });
        }

        await EnsureProductExistsAsync(productId, includeInactive: true);

        var repository = _unitOfWork.Repository<ProductImage>();
        var existingImages = (await repository.FindAsync(
                filter: image => image.ProductId == productId,
                orderBy: query => query.OrderBy(image => image.DisplayOrder).ThenBy(image => image.ImageId)))
            .ToList();
        var hasPrimaryImage = existingImages.Any(image => image.IsPrimary);
        var nextDisplayOrder = existingImages
            .Select(image => image.DisplayOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var createdImages = imageUrls
            .Select((imageUrl, index) => new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl,
                DisplayOrder = nextDisplayOrder + index,
                IsPrimary = !hasPrimaryImage && index == 0
            })
            .ToList();

        await repository.AddRangeAsync(createdImages);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductImagesResponse
        {
            Items = createdImages.Select(MapImage).ToList()
        };
    }

    public async Task<MessageResponse> UpdateImageMetadataAsync(
        int productId,
        int imageId,
        UpdateProductImageMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsPrimary.HasValue && !request.DisplayOrder.HasValue)
        {
            throw new ApiException(
                (int)HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "Invalid product image data",
                new { field = "body", issue = "At least one metadata field must be provided" });
        }

        await EnsureProductExistsAsync(productId, includeInactive: true);

        var repository = _unitOfWork.Repository<ProductImage>();
        var images = (await repository.FindAsync(
                filter: image => image.ProductId == productId,
                orderBy: query => query.OrderBy(image => image.DisplayOrder).ThenBy(image => image.ImageId)))
            .ToList();
        var image = images.FirstOrDefault(currentImage => currentImage.ImageId == imageId);

        if (image is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_IMAGE_NOT_FOUND", "Product image not found");
        }

        if (request.IsPrimary.HasValue)
        {
            if (request.IsPrimary.Value)
            {
                foreach (var currentImage in images)
                {
                    currentImage.IsPrimary = currentImage.ImageId == imageId;
                    repository.Update(currentImage);
                }
            }
            else
            {
                image.IsPrimary = false;
                repository.Update(image);
            }
        }

        if (request.DisplayOrder.HasValue)
        {
            image.DisplayOrder = request.DisplayOrder.Value;
            repository.Update(image);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Product image updated"
        };
    }

    public async Task<DeleteProductImageResult> DeleteImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId, includeInactive: true);

        var repository = _unitOfWork.Repository<ProductImage>();
        var images = (await repository.FindAsync(
                filter: image => image.ProductId == productId,
                orderBy: query => query.OrderBy(image => image.DisplayOrder).ThenBy(image => image.ImageId)))
            .ToList();
        var image = images.FirstOrDefault(currentImage => currentImage.ImageId == imageId);

        if (image is null)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_IMAGE_NOT_FOUND", "Product image not found");
        }

        var deletedImageUrl = image.ImageUrl;
        var wasPrimary = image.IsPrimary;
        repository.Remove(image);

        if (wasPrimary)
        {
            var replacementImage = images
                .Where(currentImage => currentImage.ImageId != imageId)
                .OrderBy(currentImage => currentImage.DisplayOrder)
                .ThenBy(currentImage => currentImage.ImageId)
                .FirstOrDefault();

            if (replacementImage is not null)
            {
                replacementImage.IsPrimary = true;
                repository.Update(replacementImage);
            }
        }

        // TODO: revisit physical delete strategy once image delete behavior is finalized in API_SPEC.md.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteProductImageResult
        {
            Message = "Product image deleted",
            ImageUrl = deletedImageUrl
        };
    }

    private async Task EnsureProductExistsAsync(int productId, bool includeInactive)
    {
        var productRepository = _unitOfWork.Repository<RepositoryLayer.Entities.Product>();
        var exists = await productRepository.ExistsAsync(
            product => product.ProductId == productId && (includeInactive || product.IsActive));

        if (!exists)
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND", "Product not found");
        }
    }

    private static ProductImageResponse MapImage(ProductImage image)
    {
        return new ProductImageResponse
        {
            ImageId = image.ImageId,
            ImageUrl = image.ImageUrl,
            DisplayOrder = image.DisplayOrder,
            IsPrimary = image.IsPrimary
        };
    }
}
