namespace RepositoryLayer.Entities;

public class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Category? Category { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = [];

    public ICollection<ProductImage> Images { get; set; } = [];
}
