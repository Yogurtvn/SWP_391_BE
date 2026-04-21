namespace ServiceLayer.DTOs.Orders;

public class OrderItemsResponse
{
    public List<OrderItemListItemResponse> Items { get; set; } = [];
}

public class OrderItemListItemResponse
{
    public int OrderItemId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public string? PromotionNameSnapshot { get; set; }

    public int? LensTypeId { get; set; }

    public decimal? LensPrice { get; set; }

    public int? PrescriptionId { get; set; }
}

public class OrderItemDetailResponse
{
    public int OrderItemId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public string? SelectedColor { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public string? PromotionNameSnapshot { get; set; }

    public int? LensTypeId { get; set; }

    public decimal? LensPrice { get; set; }

    public int? PrescriptionId { get; set; }
}
