namespace ServiceLayer.DTOs.Cart.Response;

public class CartItemResponse
{
    public int CartItemId { get; set; }

    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? VariantColor { get; set; }

    public string? VariantSize { get; set; }

    public string ItemType { get; set; } = string.Empty;

    public string OrderType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int StockQuantity { get; set; }

    public bool IsReadyAvailable { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }

    public string? PreOrderNote { get; set; }

    public string? SelectedColor { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public PrescriptionCartItemDetailResponse? Prescription { get; set; }
}
