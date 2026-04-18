using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class ReadyOrderCheckoutRequest
{
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
    public int ToDistrictId { get; set; }

    [Required]
    public string ToWardCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<ReadyOrderCheckoutItemRequest> Items { get; set; } = [];
}

public class ReadyOrderCheckoutItemRequest
{
    [Range(1, int.MaxValue)]
    public int VariantId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(50)]
    public string? SelectedColor { get; set; }
}
