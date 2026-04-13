namespace RepositoryLayer.Entities;

public class Payment
{
    public int PaymentId { get; set; }

    public int? OrderId { get; set; }

    public decimal? Amount { get; set; }

    public int PaymentMethodId { get; set; }

    public int PaymentStatusId { get; set; }

    public DateTime? PaidAt { get; set; }

    public Order? Order { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = null!;

    public PaymentStatus PaymentStatus { get; set; } = null!;

    public ICollection<PaymentHistory> PaymentHistories { get; set; } = [];
}
