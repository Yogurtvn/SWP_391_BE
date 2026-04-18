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

    public int? LensTypeId { get; set; }

    public decimal? LensPrice { get; set; }
}

public class OrderItemDetailResponse
{
    public int OrderItemId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public string? SelectedColor { get; set; }

    public decimal TotalPrice { get; set; }

    public int? LensTypeId { get; set; }

    public decimal? LensPrice { get; set; }
}
