namespace RepositoryLayer.Entities;

public class LensType
{
    public int LensTypeId { get; set; }

    public string LensName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
