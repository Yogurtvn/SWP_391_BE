namespace RepositoryLayer.Entities;

public class SupplyReceipt
{
    public int ReceiptId { get; set; }

    public int VariantId { get; set; }

    public int QuantityReceived { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public string? BatchNumber { get; set; }

    public int? StaffId { get; set; }

    public string? Note { get; set; }

    public ProductVariant Variant { get; set; } = null!;

    public User? Staff { get; set; }
}
