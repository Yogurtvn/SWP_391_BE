namespace ServiceLayer.DTOs.Cart.Response;

public class StandardCartItemCreatedResponse
{
    public int CartItemId { get; set; }

    public string ItemType { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
