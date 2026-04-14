using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public OrderType OrderType { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string? ShippingCode { get; set; }

    public ShippingStatus? ShippingStatus { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public int? StaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public User? Staff { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}
