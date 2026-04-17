namespace ServiceLayer.DTOs.User.Response;

/// <summary>
/// DTO trả về khi Admin tạo user mới thành công.
/// </summary>
public class CreateUserResponse
{
    // Mã định danh của user vừa tạo
    public int UserId { get; set; }

    // Vai trò được gán cho user (dạng chuỗi lowercase)
    public string Role { get; set; } = string.Empty;
}
