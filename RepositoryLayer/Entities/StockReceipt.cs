namespace RepositoryLayer.Entities;

public class StockReceipt
{
    public int ReceiptId { get; set; }

    public int VariantId { get; set; }

    public int QuantityReceived { get; set; }

    public DateTime ReceivedDate { get; set; }

    public int? StaffId { get; set; }

    public string? Note { get; set; }

    public ProductVariant Variant { get; set; } = null!;

    public User? Staff { get; set; }
}
