namespace ServiceLayer.DTOs.LensType.Response;

public class LensTypeDetailResponse
{
    public int LensTypeId { get; set; }

    public string LensCode { get; set; } = string.Empty;

    public string LensName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}
