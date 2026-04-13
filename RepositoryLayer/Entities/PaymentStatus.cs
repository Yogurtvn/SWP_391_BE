namespace RepositoryLayer.Entities;

public class PaymentStatus
{
    public int PaymentStatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public ICollection<Payment> Payments { get; set; } = [];

    public ICollection<PaymentHistory> PaymentHistories { get; set; } = [];
}
