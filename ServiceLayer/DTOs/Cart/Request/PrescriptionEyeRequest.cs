namespace ServiceLayer.DTOs.Cart.Request;

public class PrescriptionEyeRequest
{
    public decimal? Sph { get; set; }

    public decimal? Cyl { get; set; }

    public int? Axis { get; set; }
}
