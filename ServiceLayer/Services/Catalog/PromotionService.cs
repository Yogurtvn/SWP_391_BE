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
    private const int DefaultAvailablePromotionLimit = 20;
    private const int MaxAvailablePromotionLimit = 100;

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

    public async Task<IReadOnlyList<PromotionResponse>> GetAvailablePromotionsAsync(int limit = DefaultAvailablePromotionLimit, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();
        var now = DateTime.UtcNow;
        var normalizedLimit = Math.Clamp(limit, 1, MaxAvailablePromotionLimit);
        var promotions = (await repository.FindAsync(
                filter: promotion =>
                    promotion.IsActive &&
                    promotion.StartAt <= now &&
                    promotion.EndAt >= now,
                orderBy: query => query
                    .OrderByDescending(promotion => promotion.DiscountPercent)
                    .ThenBy(promotion => promotion.EndAt)
                    .ThenBy(promotion => promotion.PromotionId),
                tracked: false))
            .Take(normalizedLimit)
            .ToList();

        return promotions.Select(MapToResponse).ToList();
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
        ArgumentNullException.ThrowIfNull(request);

        var name = NormalizeRequiredText(request.Name, "name", 255);
        var description = NormalizeOptionalText(request.Description, "description", 1000);
        ValidateDiscountPercent(request.DiscountPercent);
        ValidatePromotionDates(request.StartAt, request.EndAt);

        var repository = _unitOfWork.Repository<Promotion>();
        var now = DateTime.UtcNow;

        var promotion = new Promotion
        {
            Name = name,
            Description = description,
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
        ArgumentNullException.ThrowIfNull(request);

        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        var newStartAt = request.StartAt ?? promotion.StartAt;
        var newEndAt = request.EndAt ?? promotion.EndAt;
        var newDiscountPercent = request.DiscountPercent ?? promotion.DiscountPercent;

        ValidateDiscountPercent(newDiscountPercent);
        ValidatePromotionDates(newStartAt, newEndAt);

        if (request.Name is not null)
        {
            promotion.Name = NormalizeRequiredText(request.Name, "name", 255);
        }

        if (request.Description is not null)
        {
            promotion.Description = NormalizeOptionalText(request.Description, "description", 1000);
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

    public async Task<MessageResponse> UpdatePromotionStatusAsync(
        int id,
        UpdatePromotionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsActive.HasValue)
        {
            throw CreateValidationException("isActive", "isActive is required");
        }

        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        promotion.IsActive = request.IsActive.Value;
        promotion.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Promotion status updated"
        };
    }

    public async Task DeletePromotionAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Promotion>();
        var promotion = await repository.GetFirstOrDefaultAsync(p => p.PromotionId == id, tracked: true)
            ?? throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");

        repository.Remove(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageResponse> AssignPromotionToVariantsAsync(
        int promotionId,
        AssignPromotionVariantsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variantIds = NormalizeVariantIds(request.VariantIds);
        var promotionRepository = _unitOfWork.Repository<Promotion>();

        if (!await promotionRepository.ExistsAsync(p => p.PromotionId == promotionId))
        {
            throw new ApiException((int)HttpStatusCode.NotFound, "PROMOTION_NOT_FOUND", "Promotion not found");
        }

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var variants = (await variantRepository.FindAsync(
                v => variantIds.Contains(v.VariantId),
                tracked: true))
            .ToList();
        var missingVariantIds = variantIds
            .Except(variants.Select(variant => variant.VariantId))
            .OrderBy(variantId => variantId)
            .ToList();

        if (missingVariantIds.Count > 0)
        {
            throw new ApiException(
                (int)HttpStatusCode.NotFound,
                "VARIANT_NOT_FOUND",
                "One or more variants were not found",
                new { variantIds = missingVariantIds });
        }

        foreach (var variant in variants)
        {
            variant.PromotionId = promotionId;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse
        {
            Message = "Promotion assigned to variants"
        };
    }

    public async Task AssignPromotionToVariantAsync(int promotionId, int variantId, CancellationToken cancellationToken = default)
    {
        await AssignPromotionToVariantsAsync(
            promotionId,
            new AssignPromotionVariantsRequest
            {
                VariantIds = [variantId]
            },
            cancellationToken);
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
        if (startAt == default)
        {
            throw CreateValidationException("startAt", "startAt is required");
        }

        if (endAt == default)
        {
            throw CreateValidationException("endAt", "endAt is required");
        }

        if (endAt <= startAt)
        {
            throw CreateValidationException("endAt", "endAt must be after startAt");
        }
    }

    private static void ValidateDiscountPercent(decimal discountPercent)
    {
        if (discountPercent <= 0m || discountPercent > 100m)
        {
            throw CreateValidationException(
                "discountPercent",
                "discountPercent must be greater than 0 and less than or equal to 100");
        }
    }

    private static List<int> NormalizeVariantIds(IEnumerable<int>? variantIds)
    {
        var normalizedVariantIds = variantIds?
            .Distinct()
            .ToList()
            ?? [];

        if (normalizedVariantIds.Count == 0)
        {
            throw CreateValidationException("variantIds", "variantIds is required");
        }

        if (normalizedVariantIds.Any(variantId => variantId <= 0))
        {
            throw CreateValidationException("variantIds", "each variantId must be greater than 0");
        }

        return normalizedVariantIds;
    }

    private static string NormalizeRequiredText(string? value, string field, int maxLength)
    {
        var normalizedValue = NormalizeText(value);

        if (normalizedValue is null)
        {
            throw CreateValidationException(field, $"{field} is required");
        }

        if (normalizedValue.Length > maxLength)
        {
            throw CreateValidationException(field, $"{field} must not exceed {maxLength} characters");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(string? value, string field, int maxLength)
    {
        var normalizedValue = NormalizeText(value);

        if (normalizedValue is not null && normalizedValue.Length > maxLength)
        {
            throw CreateValidationException(field, $"{field} must not exceed {maxLength} characters");
        }

        return normalizedValue;
    }

    private static string? NormalizeText(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static ApiException CreateValidationException(string field, string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            "VALIDATION_ERROR",
            "Invalid promotion data",
            new { field, issue });
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
