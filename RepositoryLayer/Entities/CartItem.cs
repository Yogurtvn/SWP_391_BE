using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class CartItem
{
    public int CartItemId { get; set; }

    public int CartId { get; set; }

    public int VariantId { get; set; }

    public CartItemType ItemType { get; set; }

    public OrderType OrderType { get; set; }

    public int Quantity { get; set; }

    public string? SelectedColor { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Cart Cart { get; set; } = null!;

    public ProductVariant Variant { get; set; } = null!;

    public CartPrescriptionDetail? CartPrescriptionDetail { get; set; }
}
