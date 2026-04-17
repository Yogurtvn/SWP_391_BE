namespace ServiceLayer.DTOs.Cart.Response;

public class PrescriptionCartItemCreatedResponse
{
    public int CartItemId { get; set; }

    public string ItemType { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public decimal FramePrice { get; set; }

    public decimal LensPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
