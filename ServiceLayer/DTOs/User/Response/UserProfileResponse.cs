namespace ServiceLayer.DTOs.User.Response;

/// <summary>
/// DTO trả về thông tin profile của user hiện tại.
/// </summary>
public class UserProfileResponse
{
    // Mã định danh của user
    public int UserId { get; set; }

    // Địa chỉ email của user
    public string Email { get; set; } = string.Empty;

    // Họ tên đầy đủ của user
    public string? FullName { get; set; }

    // Số điện thoại của user
    public string? Phone { get; set; }
}
