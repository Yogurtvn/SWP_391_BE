using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.ProductVariant.Request;
using ServiceLayer.DTOs.ProductVariant.Response;

namespace ServiceLayer.Contracts.ProductVariant;

public interface IProductVariantService
{
    Task<PagedResult<ProductVariantListItemResponse>> GetVariantsByProductAsync(
        int productId,
        GetProductVariantsRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ProductVariantDetailResponse?> GetVariantByIdAsync(
        int variantId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ProductVariantIdResponse> CreateVariantAsync(
        int productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateVariantAsync(
        int variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateVariantStatusAsync(
        int variantId,
        UpdateProductVariantStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default);
}
