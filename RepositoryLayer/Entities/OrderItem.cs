namespace RepositoryLayer.Entities;

public class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public string? SelectedColor { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal OriginalUnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalUnitPrice { get; set; }

    public string? PromotionNameSnapshot { get; set; }

    public int? LensTypeId { get; set; }

    public decimal? LensPrice { get; set; }

    public int? PrescriptionId { get; set; }

    public Order Order { get; set; } = null!;

    public ProductVariant Variant { get; set; } = null!;

    public LensType? LensType { get; set; }

    public PrescriptionSpec? Prescription { get; set; }
}
