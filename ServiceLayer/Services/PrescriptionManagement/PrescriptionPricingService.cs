using Microsoft.Extensions.Options;
using ServiceLayer.Configuration;
using ServiceLayer.Contracts.Prescription;
using ServiceLayer.Exceptions;
using System.Net;

namespace ServiceLayer.Services.PrescriptionManagement;

public class PrescriptionPricingService(IOptions<PrescriptionPricingOptions> options) : IPrescriptionPricingService
{
    private readonly PrescriptionPricingOptions _options = options.Value;

    public PrescriptionPriceCalculation Calculate(
        decimal framePrice,
        decimal lensBasePrice,
        string? lensMaterial,
        IReadOnlyCollection<string>? coatings,
        int quantity,
        string errorCode,
        string errorMessage)
    {
        if (quantity <= 0)
        {
            throw CreateApiException(errorCode, errorMessage, "quantity", "quantity must be greater than 0");
        }

        var normalizedLensMaterial = NormalizeOptionToken(lensMaterial);
        var normalizedCoatings = NormalizeCoatings(coatings);
        var materialPriceAdjustments = BuildNormalizedPriceMap(_options.MaterialPriceAdjustments);
        var coatingPriceAdjustments = BuildNormalizedPriceMap(_options.CoatingPriceAdjustments);

        var materialPrice = ResolveOptionPrice(
            normalizedLensMaterial,
            materialPriceAdjustments,
            "lensMaterial",
            errorCode,
            errorMessage);
        var coatingPrice = normalizedCoatings.Sum(coating =>
            ResolveOptionPrice(
                coating,
                coatingPriceAdjustments,
                "coatings",
                errorCode,
                errorMessage));
        var lensPrice = lensBasePrice + materialPrice + coatingPrice;

        return new PrescriptionPriceCalculation
        {
            LensMaterial = normalizedLensMaterial,
            Coatings = normalizedCoatings,
            FramePrice = framePrice,
            LensBasePrice = lensBasePrice,
            MaterialPrice = materialPrice,
            CoatingPrice = coatingPrice,
            LensPrice = lensPrice,
            TotalPrice = (framePrice + lensPrice) * quantity
        };
    }

    private static string? NormalizeOptionToken(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static IReadOnlyList<string> NormalizeCoatings(IReadOnlyCollection<string>? coatings)
    {
        if (coatings is null || coatings.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(coatings.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var coating in coatings)
        {
            var normalizedCoating = NormalizeOptionToken(coating);
            if (normalizedCoating is null || !seen.Add(normalizedCoating))
            {
                continue;
            }

            normalized.Add(normalizedCoating);
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, decimal> BuildNormalizedPriceMap(IReadOnlyDictionary<string, decimal>? source)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (source is null || source.Count == 0)
        {
            return result;
        }

        foreach (var (key, value) in source)
        {
            var normalizedKey = NormalizeOptionToken(key);
            if (normalizedKey is null)
            {
                continue;
            }

            result[normalizedKey] = value;
        }

        return result;
    }

    private static decimal ResolveOptionPrice(
        string? optionValue,
        IReadOnlyDictionary<string, decimal> configuredPrices,
        string field,
        string errorCode,
        string errorMessage)
    {
        if (optionValue is null)
        {
            return 0m;
        }

        if (!configuredPrices.TryGetValue(optionValue, out var price))
        {
            throw CreateApiException(
                errorCode,
                errorMessage,
                field,
                $"{field} '{optionValue}' is not supported by pricing configuration");
        }

        return price;
    }

    private static ApiException CreateApiException(
        string errorCode,
        string errorMessage,
        string field,
        string issue)
    {
        return new ApiException(
            (int)HttpStatusCode.BadRequest,
            errorCode,
            errorMessage,
            new { field, issue });
    }
}
