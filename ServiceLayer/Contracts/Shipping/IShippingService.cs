using ServiceLayer.DTOs.Shipping.Request;
using ServiceLayer.DTOs.Shipping.Response;

namespace ServiceLayer.Contracts.Shipping;

/// <summary>
/// Interface vận chuyển chuẩn GHN (Phiên bản Portable)
/// </summary>
public interface IShippingService
{
    // API địa chỉ
    Task<List<GhnProvinceResponse>> GetProvincesAsync(CancellationToken ct = default);
    Task<List<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken ct = default);
    Task<List<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken ct = default);

    // API tính phí (Tự động chọn gói Standard)
    Task<ShippingFeeResponse> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, CancellationToken ct = default);
}
