namespace ServiceLayer.DTOs.Cart.Request;

public class AddStandardCartItemRequest
{
    public int? VariantId { get; set; }

    public int? Quantity { get; set; }

    public string? OrderType { get; set; }
}
