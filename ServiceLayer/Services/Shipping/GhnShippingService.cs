using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceLayer.Contracts.Shipping;
using ServiceLayer.DTOs.Shipping;
using ServiceLayer.DTOs.Shipping.Request;
using ServiceLayer.DTOs.Shipping.Response;

namespace ServiceLayer.Services.Shipping;

/// <summary>
/// Triển khai dịch vụ GHN (Mô hình Portable - Chỉ hỗ trợ gói Standard)
/// Đặc điểm: Tự động tìm mã dịch vụ (service_id) phù hợp với địa chỉ để tính phí.
/// </summary>
public class GhnShippingService(
    IHttpClientFactory httpClientFactory, 
    IOptions<GhnSettings> ghnSettings,
    ILogger<GhnShippingService> logger
) : IShippingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("GHN");
    private readonly GhnSettings _settings = ghnSettings.Value;
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

    /// <summary>
    /// Tính phí vận chuyển (Tự động tìm dịch vụ Standard của GHN)
    /// </summary>
    public async Task<ShippingFeeResponse> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, CancellationToken ct = default)
    {
        // 1. Tìm danh sách dịch vụ khả dụng cho tuyến đường này
        var availableServices = await GetInternalAvailableServicesAsync(request.ToDistrictId, ct);
        
        // 2. Lọc lấy dịch vụ thuộc nhóm "Tiêu chuẩn" (Standard - service_type_id = 2)
        // Nếu không thấy nhóm 2 thì lấy dịch vụ đầu tiên tìm được làm dự phòng
        var standardService = availableServices.FirstOrDefault(s => s.ServiceTypeId == 2) 
                             ?? availableServices.FirstOrDefault();

        if (standardService == null)
        {
            throw new InvalidOperationException("GHN không hỗ trợ dịch vụ vận chuyển cho tuyến đường này.");
        }

        // 3. Gọi API tính phí với service_id tìm được
        var feeRequestBody = new
        {
            from_district_id = _settings.FromDistrictId,
            from_ward_code = _settings.FromWardCode,
            service_id = standardService.ServiceId,
            service_type_id = 2, // Mặc định là gói Standard (Hàng nhẹ)
            to_district_id = request.ToDistrictId,
            to_ward_code = request.ToWardCode,
            weight = request.Weight,
            height = 10,  // Mặc định 10cm
            length = 10,  // Mặc định 10cm
            width = 10,   // Mặc định 10cm
            insurance_value = 0 // Mặc định không bảo hiểm
        };

        var resp = await _httpClient.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/fee", feeRequestBody, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("GHN Fee Error: {Error}", errorContent);
            throw new InvalidOperationException("Địa chỉ không hợp lệ hoặc GHN từ chối tính phí.");
        }

        var result = await resp.Content.ReadFromJsonAsync<GhnApiResponse<GhnFeeData>>(JsonOptions, ct);
        
        return new ShippingFeeResponse
        {
            TotalFee = result?.Data?.Total ?? 0,
            ServiceFee = result?.Data?.ServiceFee ?? 0,
            InsuranceFee = result?.Data?.InsuranceFee ?? 0,
            ExpectedDeliveryTime = "2-5 ngày (Tiêu chuẩn)"
        };
    }

    // Helper nội bộ: Lấy danh sách dịch vụ giữa Shop và khách
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
}

// ===== Các class nội bộ phục vụ Deserialize GHN API =====

internal class GhnApiResponse<T> { public int Code { get; set; } public string Message { get; set; } = ""; public T? Data { get; set; } }

internal class GhnFeeData { public decimal Total { get; set; } public decimal Service_fee { get; set; } public decimal Insurance_fee { get; set; } public decimal ServiceFee => Service_fee; public decimal InsuranceFee => Insurance_fee; }

public class GhnAvailableServiceResponse 
{ 
    public int ServiceId { get; set; } 
    public string ShortName { get; set; } = ""; 
    public int ServiceTypeId { get; set; } 
}
