using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Promotions;

namespace ServiceLayer.Contracts.Catalog;

public interface IPromotionService
{
    Task<PagedResult<PromotionResponse>> GetPromotionsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> GetPromotionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> UpdatePromotionAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> UpdatePromotionStatusAsync(int id, UpdatePromotionStatusRequest request, CancellationToken cancellationToken = default);
    Task DeletePromotionAsync(int id, CancellationToken cancellationToken = default);
    Task<MessageResponse> AssignPromotionToVariantsAsync(int promotionId, AssignPromotionVariantsRequest request, CancellationToken cancellationToken = default);
    Task AssignPromotionToVariantAsync(int promotionId, int variantId, CancellationToken cancellationToken = default);
    Task RemovePromotionFromVariantAsync(int variantId, CancellationToken cancellationToken = default);
}
