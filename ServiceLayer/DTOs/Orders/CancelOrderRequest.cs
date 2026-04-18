using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Orders;

public class CancelOrderRequest
{
    [MaxLength(255)]
    public string? Reason { get; set; }
}
