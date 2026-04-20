using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Catalog;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.Promotions;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.Catalog;

public class PromotionService(IUnitOfWork unitOfWork) : IPromotionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PagedResult<PromotionResponse>> GetPromotionsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();

        var pagedResult = await repository.GetPagedAsync(
            request,
            orderBy: q => q.OrderByDescending(p => p.CreatedAt),
            tracked: false,
            cancellationToken: cancellationToken);

        return PagedResult<PromotionResponse>.Create(
            pagedResult.Items.Select(MapToResponse).ToList(),
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalItems);
    }

    public async Task<PromotionResponse> GetPromotionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: false)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        return MapToResponse(promotion);
    }

    public async Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePromotionDates(request.StartAt, request.EndAt);

        var repository = _unitOfWork.Repository<Promotion>();
        var now = DateTime.UtcNow;

        var promotion = new Promotion
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DiscountPercent = request.DiscountPercent,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(promotion);
    }

    public async Task<PromotionResponse> UpdatePromotionAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        var newStartAt = request.StartAt ?? promotion.StartAt;
        var newEndAt = request.EndAt ?? promotion.EndAt;
        ValidatePromotionDates(newStartAt, newEndAt);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            promotion.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            promotion.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.DiscountPercent.HasValue)
        {
            promotion.DiscountPercent = request.DiscountPercent.Value;
        }

        if (request.StartAt.HasValue)
        {
            promotion.StartAt = request.StartAt.Value;
        }

        if (request.EndAt.HasValue)
        {
            promotion.EndAt = request.EndAt.Value;
        }

        if (request.IsActive.HasValue)
        {
            promotion.IsActive = request.IsActive.Value;
        }

        promotion.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(promotion);
    }

    public async Task DeletePromotionAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        repository.Remove(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignPromotionToVariantAsync(int promotionId, int variantId, CancellationToken cancellationToken = default)
    {
        var promotionRepository = _unitOfWork.Repository<Promotion>();
        if (!await promotionRepository.ExistsAsync(p => p.PromotionId == promotionId))
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");
        }

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var variant = await variantRepository.GetFirstOrDefaultAsync(v => v.VariantId == variantId, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "VARIANT_NOT_FOUND", "Variant not found");

        variant.PromotionId = promotionId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePromotionFromVariantAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var variant = await variantRepository.GetFirstOrDefaultAsync(v => v.VariantId == variantId, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "VARIANT_NOT_FOUND", "Variant not found");

        variant.PromotionId = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePromotionDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ApiException((int)HttpStatusCode.BadRequest, "INVALID_PROMOTION_DATES", "End date must be after start date");
        }
    }

    private static PromotionResponse MapToResponse(Promotion promotion)
    {
        return new PromotionResponse
        {
            PromotionId = promotion.PromotionId,
            Name = promotion.Name,
            Description = promotion.Description,
            DiscountPercent = promotion.DiscountPercent,
            StartAt = promotion.StartAt,
            EndAt = promotion.EndAt,
            IsActive = promotion.IsActive,
            CreatedAt = promotion.CreatedAt,
            UpdatedAt = promotion.UpdatedAt
        };
    }
}
