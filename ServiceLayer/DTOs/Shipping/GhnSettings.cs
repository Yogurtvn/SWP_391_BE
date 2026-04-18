namespace ServiceLayer.DTOs.Shipping;

// Class cấu hình chứa thông tin kết nối tới GHN API, được bind từ appsettings.json section "GHN"
public class GhnSettings
{
    public const string SectionName = "GHN";  // Tên section trong appsettings.json

    public string BaseUrl { get; set; } = string.Empty; // URL gốc của GHN API (VD: https://online-gateway.ghn.vn/shiip/public-api)

    public string Token { get; set; } = string.Empty;   // Token xác thực GHN (lấy từ trang dev.ghn.vn)

    public int ShopId { get; set; }                      // Mã shop đã đăng ký trên GHN

    public int FromDistrictId { get; set; }              // Mã quận/huyện nơi Shop đặt (địa chỉ gửi hàng)

    public string FromWardCode { get; set; } = string.Empty; // Mã phường/xã nơi Shop đặt (địa chỉ gửi hàng)
}
