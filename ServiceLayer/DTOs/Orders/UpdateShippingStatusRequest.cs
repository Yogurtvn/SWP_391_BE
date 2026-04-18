using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class UpdateShippingStatusRequest
{
    [Required]
    [MaxLength(30)]
    public string ShippingStatus { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ShippingCode { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
}
