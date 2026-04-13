namespace RepositoryLayer.Entities;

public class Inventory
{
    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public int? ReservedQuantity { get; set; }

    public bool? IsPreOrderAllowed { get; set; }

    public ProductVariant Variant { get; set; } = null!;
}
