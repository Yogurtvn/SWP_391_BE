using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.User.Request;

/// <summary>
/// DTO nhận dữ liệu từ client khi user cập nhật profile của chính mình.
/// </summary>
public class UpdateProfileRequest
{
    // Họ tên đầy đủ - bắt buộc nhập
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    // Số điện thoại - phải đúng định dạng cơ bản (10-11 số, bắt đầu bằng 0)
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone must be a valid Vietnamese phone number")]
    public string? Phone { get; set; }
}
