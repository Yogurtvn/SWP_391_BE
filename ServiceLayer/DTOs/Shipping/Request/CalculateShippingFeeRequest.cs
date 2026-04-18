using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Shipping.Request;

/// <summary>
/// Yêu cầu tính phí vận chuyển (Rút gọn cho module Portable)
/// Chỉ cần thông tin địa chỉ và cân nặng.
/// </summary>
public class CalculateShippingFeeRequest
{
    [Required]
    public int ToDistrictId { get; set; }        // Mã Quận/Huyện nhận

    [Required]
    public string ToWardCode { get; set; } = string.Empty; // Mã Phường/Xã nhận

    [Range(1, 50000)]
    public int Weight { get; set; } = 200;       // Trọng lượng (gram)
}
