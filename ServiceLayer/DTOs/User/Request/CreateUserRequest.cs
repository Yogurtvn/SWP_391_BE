using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.User.Request;

/// <summary>
/// DTO nhận dữ liệu từ Admin khi tạo tài khoản staff hoặc admin mới.
/// </summary>
public class CreateUserRequest
{
    // Email đăng nhập - bắt buộc, phải duy nhất trong hệ thống
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    // Mật khẩu đăng nhập - bắt buộc
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    // Họ tên đầy đủ - bắt buộc
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    // Số điện thoại (không bắt buộc)
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone must be a valid Vietnamese phone number")]
    public string? Phone { get; set; }

    // Vai trò được gán: "admin", "staff" (bắt buộc)
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}
