namespace RepositoryLayer.Entities;

public class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int? OrderId { get; set; }

    public int OrderStatusId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Order? Order { get; set; }

    public OrderStatus OrderStatus { get; set; } = null!;

    public User? UpdatedByUser { get; set; }
}
