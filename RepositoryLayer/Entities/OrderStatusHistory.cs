using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int OrderId { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public int? UpdatedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;

    public User? UpdatedByUser { get; set; }
}
