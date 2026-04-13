namespace RepositoryLayer.Entities;

public class OrderStatus
{
    public int OrderStatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = [];
}
