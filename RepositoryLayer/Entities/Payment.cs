using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;

    public ICollection<PaymentHistory> PaymentHistories { get; set; } = [];
}
