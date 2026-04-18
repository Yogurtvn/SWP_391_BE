namespace ServiceLayer.DTOs.Shipping.Response;

// DTO ánh xạ dữ liệu Phường/Xã từ GHN API trả về
public class GhnWardResponse
{
    public string WardCode { get; set; } = string.Empty; // Mã phường/xã (string, VD: "21012")

    public string WardName { get; set; } = string.Empty; // Tên phường/xã

    public int DistrictID { get; set; }                  // Mã quận/huyện mà phường này thuộc về
}
