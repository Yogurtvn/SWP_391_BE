namespace ServiceLayer.DTOs.Shipping.Response;

// DTO ánh xạ dữ liệu Tỉnh/Thành phố từ GHN API trả về
public class GhnProvinceResponse
{
    public int ProvinceID { get; set; }       // Mã tỉnh/thành phố (VD: 202 = Hồ Chí Minh)

    public string ProvinceName { get; set; } = string.Empty; // Tên tỉnh/thành phố
}
