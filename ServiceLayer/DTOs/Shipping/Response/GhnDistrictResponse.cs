namespace ServiceLayer.DTOs.Shipping.Response;

// DTO ánh xạ dữ liệu Quận/Huyện từ GHN API trả về
public class GhnDistrictResponse
{
    public int DistrictID { get; set; }       // Mã quận/huyện (VD: 1442 = Quận 1)

    public string DistrictName { get; set; } = string.Empty; // Tên quận/huyện

    public int ProvinceID { get; set; }       // Mã tỉnh/thành phố mà quận này thuộc về
}
