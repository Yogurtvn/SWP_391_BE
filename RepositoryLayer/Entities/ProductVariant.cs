namespace RepositoryLayer.Entities;

public class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? FrameType { get; set; }

    public string? Size { get; set; }

    public string? Color { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public Product Product { get; set; } = null!;

    public Inventory? Inventory { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<StockReceipt> StockReceipts { get; set; } = [];
}
