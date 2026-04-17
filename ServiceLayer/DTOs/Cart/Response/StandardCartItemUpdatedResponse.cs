namespace ServiceLayer.DTOs.Cart.Response;

public class StandardCartItemUpdatedResponse
{
    public string Message { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }
}
