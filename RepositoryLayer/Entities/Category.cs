namespace RepositoryLayer.Entities;

public class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}
