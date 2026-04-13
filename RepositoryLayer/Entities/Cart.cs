namespace RepositoryLayer.Entities;

public class Cart
{
    public int CartId { get; set; }

    public int? UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = [];
}
