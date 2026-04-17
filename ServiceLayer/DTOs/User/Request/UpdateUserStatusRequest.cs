using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.User.Request;

/// <summary>
/// DTO nhận dữ liệu khi Admin khóa/mở khóa tài khoản user.
/// </summary>
public class UpdateUserStatusRequest
{
    // Trạng thái mới: true = mở khóa, false = khóa tài khoản
    [Required(ErrorMessage = "IsActive is required")]
    public bool IsActive { get; set; }
}
