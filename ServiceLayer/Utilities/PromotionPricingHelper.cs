using RepositoryLayer.Entities;

namespace ServiceLayer.Utilities;

internal static class PromotionPricingHelper
{
    public static PromotionPricingSnapshot Calculate(ProductVariant variant, DateTime currentTime)
    {
        ArgumentNullException.ThrowIfNull(variant);

        return Calculate(variant.Price, variant.Promotion, currentTime);
    }

    public static PromotionPricingSnapshot Calculate(decimal originalPrice, Promotion? promotion, DateTime currentTime)
    {
        var discountPercent = IsApplicable(promotion, currentTime)
            ? promotion!.DiscountPercent
            : 0m;
        var priceForCalculation = Math.Max(0m, originalPrice);
        var discountAmount = Math.Round(priceForCalculation * discountPercent / 100m, 2);
        var finalPrice = Math.Max(0m, priceForCalculation - discountAmount);

        return new PromotionPricingSnapshot(
            OriginalPrice: originalPrice,
            DiscountPercent: discountPercent,
            DiscountAmount: discountAmount,
            FinalPrice: finalPrice,
            PromotionName: discountPercent > 0m ? promotion!.Name : null);
    }

    private static bool IsApplicable(Promotion? promotion, DateTime currentTime)
    {
        return promotion is { IsActive: true }
            && promotion.DiscountPercent > 0m
            && promotion.DiscountPercent <= 100m
            && promotion.StartAt <= currentTime
            && promotion.EndAt >= currentTime;
    }
}

internal sealed record PromotionPricingSnapshot(
    decimal OriginalPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal FinalPrice,
    string? PromotionName);
