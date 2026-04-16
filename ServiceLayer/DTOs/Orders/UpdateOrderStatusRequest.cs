using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    [Required]
    [MaxLength(30)]
    public string OrderStatus { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ShippingStatus { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }
}
