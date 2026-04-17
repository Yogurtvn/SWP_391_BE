using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Product.Request;
using ServiceLayer.DTOs.Product.Response;

namespace ServiceLayer.Contracts.Product;

public interface IProductService
{
    Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        GetProductsRequest request,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ProductDetailResponse?> GetProductByIdAsync(
        int productId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<ProductIdResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateProductAsync(int productId, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateProductStatusAsync(int productId, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);

    Task<MessageResponse> DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
}
