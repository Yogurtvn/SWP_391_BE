namespace ServiceLayer.DTOs.Inventory.Response;

public class InventoryListDtoResponse
{
    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public bool IsReadyAvailable { get; set; }

    public bool IsPreOrderAllowed { get; set; }

    public DateTime? ExpectedRestockDate { get; set; }

    public string? PreOrderNote { get; set; }
}
