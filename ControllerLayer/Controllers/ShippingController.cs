using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Shipping;
using ServiceLayer.DTOs.Shipping.Request;
using ServiceLayer.DTOs.Shipping.Response;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller Vận chuyển (Phiên bản Portable - Standard Only)
/// </summary>
[Route("api/shipping")]
[ApiController]
public class ShippingController(IShippingService shippingService) : ControllerBase
{
    private readonly IShippingService _shippingService = shippingService;

    [AllowAnonymous]
    [HttpGet("provinces")]
    public async Task<ActionResult<List<GhnProvinceResponse>>> GetProvinces(CancellationToken ct) 
        => Ok(await _shippingService.GetProvincesAsync(ct));

    [AllowAnonymous]
    [HttpGet("districts")]
    public async Task<ActionResult<List<GhnDistrictResponse>>> GetDistricts([FromQuery] int provinceId, CancellationToken ct) 
        => Ok(await _shippingService.GetDistrictsAsync(provinceId, ct));

    [AllowAnonymous]
    [HttpGet("wards")]
    public async Task<ActionResult<List<GhnWardResponse>>> GetWards([FromQuery] int districtId, CancellationToken ct) 
        => Ok(await _shippingService.GetWardsAsync(districtId, ct));

    /// <summary>
    /// Tính phí vận chuyển (Tự động chọn gói Standard)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("calculate-fee")]
    public async Task<ActionResult<ShippingFeeResponse>> CalculateFee([FromBody] CalculateShippingFeeRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _shippingService.CalculateShippingFeeAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
