namespace RepositoryLayer.Entities;

public class ProductImage
{
    public int ImageId { get; set; }

    public int? ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public bool? Is3D { get; set; }

    public Product? Product { get; set; }
}
