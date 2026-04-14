namespace RepositoryLayer.Entities;

public class Inventory
{
    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }

    public string? PreOrderNote { get; set; }

    public ProductVariant Variant { get; set; } = null!;
}
