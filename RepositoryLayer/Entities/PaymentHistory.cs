using RepositoryLayer.Enums;

namespace RepositoryLayer.Entities;

public class PaymentHistory
{
    public int PaymentHistoryId { get; set; }

    public int PaymentId { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public string? TransactionCode { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Payment Payment { get; set; } = null!;

}
