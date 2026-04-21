namespace ServiceLayer.DTOs.Cart.Response;

public class CartItemResponse
{
    public int CartItemId { get; set; }

    public int VariantId { get; set; }

    public string ItemType { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? SelectedColor { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public PrescriptionCartItemDetailResponse? Prescription { get; set; }
}
