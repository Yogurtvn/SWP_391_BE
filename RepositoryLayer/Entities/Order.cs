namespace RepositoryLayer.Entities;

public class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public int? PromotionId { get; set; }

    public int OrderTypeId { get; set; }

    public int OrderStatusId { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? ReceiverName { get; set; }

    public string? ReceiverPhone { get; set; }

    public string? ShippingAddress { get; set; }

    public string? ShippingCode { get; set; }

    public int? ShippingStatusId { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public int? SalesStaffId { get; set; }

    public int? OperationsStaffId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public User? User { get; set; }

    public Promotion? Promotion { get; set; }

    public OrderType OrderType { get; set; } = null!;

    public OrderStatus OrderStatus { get; set; } = null!;

    public ShippingStatus? ShippingStatus { get; set; }

    public User? SalesStaff { get; set; }

    public User? OperationsStaff { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];

    public ICollection<Return> Returns { get; set; } = [];
}
