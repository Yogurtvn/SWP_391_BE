using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepositoryLayer.Entities;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Shipping;
using ServiceLayer.DTOs.Shipping;
using ServiceLayer.DTOs.Shipping.Request;
using ServiceLayer.DTOs.Shipping.Response;

namespace ServiceLayer.Services.Shipping;

public class GhnShippingService(
    IHttpClientFactory httpClientFactory,
    IOptions<GhnSettings> ghnSettings,
    IUnitOfWork unitOfWork,
    ILogger<GhnShippingService> logger
) : IShippingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("GHN");
    private readonly GhnSettings _settings = ghnSettings.Value;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<GhnShippingService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<GhnProvinceResponse>> GetProvincesAsync(CancellationToken ct = default)
    {
        var resp = await _httpClient.GetAsync("/shiip/public-api/master-data/province", ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<List<GhnProvinceResponse>>>(JsonOptions, ct);
        return result?.Data ?? [];
    }

    public async Task<List<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken ct = default)
    {
        var resp = await _httpClient.PostAsJsonAsync("/shiip/public-api/master-data/district", new { province_id = provinceId }, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<List<GhnDistrictResponse>>>(JsonOptions, ct);
        return result?.Data ?? [];
    }

    public async Task<List<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken ct = default)
    {
        var resp = await _httpClient.PostAsJsonAsync("/shiip/public-api/master-data/ward", new { district_id = districtId }, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<List<GhnWardResponse>>>(JsonOptions, ct);
        return result?.Data ?? [];
    }

    public async Task<ShippingFeeResponse> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("items must not be empty.");
        }

        if (request.Items.Any(item => item.VariantId <= 0 || item.Quantity <= 0))
        {
            throw new InvalidOperationException("variantId and quantity must be greater than 0.");
        }

        var normalizedItems = request.Items
            .GroupBy(item => item.VariantId)
            .Select(group => new VariantQuantity(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        var variantIds = normalizedItems
            .Select(item => item.VariantId)
            .ToList();

        var variantRepository = _unitOfWork.Repository<ProductVariant>();
        var variants = (await variantRepository.FindAsync(
            filter: variant => variantIds.Contains(variant.VariantId),
            tracked: false)).ToList();

        var variantById = variants.ToDictionary(variant => variant.VariantId);

        if (variantById.Count != variantIds.Count)
        {
            var missingVariantIds = variantIds
                .Where(variantId => !variantById.ContainsKey(variantId))
                .OrderBy(variantId => variantId)
                .ToArray();

            throw new InvalidOperationException($"Variant not found: {string.Join(", ", missingVariantIds)}.");
        }

        var package = BuildShippingPackage(normalizedItems, variantById);

        var availableServices = await GetInternalAvailableServicesAsync(request.ToDistrictId, ct);
        var standardService = availableServices.FirstOrDefault(service => service.ServiceTypeId == 2)
                              ?? availableServices.FirstOrDefault();

        if (standardService is null)
        {
            throw new InvalidOperationException("GHN does not support shipping for this route.");
        }

        var feeRequestBody = new
        {
            from_district_id = _settings.FromDistrictId,
            from_ward_code = _settings.FromWardCode,
            service_id = standardService.ServiceId,
            service_type_id = 2,
            to_district_id = request.ToDistrictId,
            to_ward_code = request.ToWardCode,
            weight = package.TotalWeightGram,
            height = package.PackageHeightCm,
            length = package.PackageLengthCm,
            width = package.PackageWidthCm,
            insurance_value = 0
        };

        var resp = await _httpClient.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/fee", feeRequestBody, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("GHN Fee Error: {Error}", errorContent);
            throw new InvalidOperationException("Invalid destination address or GHN rejected the fee calculation.");
        }

        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<GhnFeeData>>(JsonOptions, ct);

        return new ShippingFeeResponse
        {
            TotalFee = result?.Data?.Total ?? 0,
            ServiceFee = result?.Data?.ServiceFee ?? 0,
            InsuranceFee = result?.Data?.InsuranceFee ?? 0,
            ExpectedDeliveryTime = "2-5 days (Standard)"
        };
    }

    private static ShippingPackageData BuildShippingPackage(
        IReadOnlyCollection<VariantQuantity> items,
        IReadOnlyDictionary<int, ProductVariant> variantById)
    {
        long totalWeight = 0;
        long totalHeight = 0;
        var packageLength = 0;
        var packageWidth = 0;

        foreach (var item in items)
        {
            var variant = variantById[item.VariantId];

            totalWeight += (long)variant.WeightGram * item.Quantity;
            totalHeight += (long)variant.PackageHeightCm * item.Quantity;
            packageLength = Math.Max(packageLength, variant.PackageLengthCm);
            packageWidth = Math.Max(packageWidth, variant.PackageWidthCm);
        }

        if (totalWeight > int.MaxValue)
        {
            throw new InvalidOperationException("Total weight is too large.");
        }

        if (totalHeight > int.MaxValue)
        {
            throw new InvalidOperationException("Package height is too large.");
        }

        return new ShippingPackageData(
            TotalWeightGram: (int)totalWeight,
            PackageLengthCm: packageLength,
            PackageWidthCm: packageWidth,
            PackageHeightCm: Math.Max(1, (int)totalHeight));
    }

    private async Task<List<GhnAvailableServiceResponse>> GetInternalAvailableServicesAsync(int toDistrictId, CancellationToken ct)
    {
        var body = new
        {
            shop_id = _settings.ShopId,
            from_district = _settings.FromDistrictId,
            to_district = toDistrictId
        };

        var resp = await _httpClient.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/available-services", body, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<List<GhnAvailableServiceResponse>>>(JsonOptions, ct);

        return result?.Data ?? [];
    }

    private sealed record VariantQuantity(int VariantId, int Quantity);

    private sealed record ShippingPackageData(
        int TotalWeightGram,
        int PackageLengthCm,
        int PackageWidthCm,
        int PackageHeightCm);
}

internal class GhnApiResponse<T> { public int Code { get; set; } public string Message { get; set; } = ""; public T? Data { get; set; } }

internal class GhnFeeData { public decimal Total { get; set; } public decimal Service_fee { get; set; } public decimal Insurance_fee { get; set; } public decimal ServiceFee => Service_fee; public decimal InsuranceFee => Insurance_fee; }

public class GhnAvailableServiceResponse
{
    public int ServiceId { get; set; }
    public string ShortName { get; set; } = "";
    public int ServiceTypeId { get; set; }
}
