using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class BuyNowOrderRequest
{
    [Range(1, int.MaxValue)]
    public int VariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(255)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;
}
