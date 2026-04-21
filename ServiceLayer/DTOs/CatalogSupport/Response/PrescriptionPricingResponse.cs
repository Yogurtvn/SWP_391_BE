namespace ServiceLayer.DTOs.CatalogSupport.Response;

public class PrescriptionPricingResponse
{
    public decimal FramePrice { get; set; }

    public decimal LensBasePrice { get; set; }

    public decimal MaterialPrice { get; set; }

    public decimal CoatingPrice { get; set; }

    public decimal LensPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }
}
