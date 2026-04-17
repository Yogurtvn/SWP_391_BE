namespace ServiceLayer.DTOs.Cart.Response;

public class CartDetailResponse
{
    public int CartId { get; set; }

    public List<CartItemResponse> Items { get; set; } = [];

    public decimal SubTotal { get; set; }
}
