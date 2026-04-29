using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Shipping.Request;

public class CalculateShippingFeeRequest
{
    [Range(1, int.MaxValue)]
    public int ToDistrictId { get; set; }

    [Required]
    [MinLength(1)]
    public string ToWardCode { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<CalculateShippingFeeItemRequest> Items { get; set; } = [];
}

public class CalculateShippingFeeItemRequest
{
    [Range(1, int.MaxValue)]
    public int VariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
