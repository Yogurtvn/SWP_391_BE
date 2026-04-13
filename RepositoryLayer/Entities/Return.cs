namespace RepositoryLayer.Entities;

public class Return
{
    public int ReturnId { get; set; }

    public int? OrderId { get; set; }

    public string? Reason { get; set; }

    public decimal? RefundAmount { get; set; }

    public int ReturnStatusId { get; set; }

    public Order? Order { get; set; }

    public ReturnStatus ReturnStatus { get; set; } = null!;
}
