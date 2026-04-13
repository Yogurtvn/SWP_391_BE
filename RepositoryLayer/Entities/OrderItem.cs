namespace RepositoryLayer.Entities;

public class OrderItem
{
    public int OrderItemId { get; set; }

    public int? OrderId { get; set; }

    public int? VariantId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public int? PrescriptionId { get; set; }

    public Order? Order { get; set; }

    public ProductVariant? Variant { get; set; }

    public PrescriptionSpec? Prescription { get; set; }
}
