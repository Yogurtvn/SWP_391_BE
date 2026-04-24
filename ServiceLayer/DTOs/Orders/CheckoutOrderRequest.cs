using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class CheckoutOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<int> CartItemIds { get; set; } = [];

    [MaxLength(30)]
    public string? OrderType { get; set; }

    [Required]
    [MaxLength(255)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal ShippingFee { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? VoucherCode { get; set; }
}
