namespace ServiceLayer.DTOs.User.Response;

/// <summary>
/// DTO trả về từng item trong danh sách users (dùng cho API Get Users - Admin).
/// </summary>
public class UserListItemResponse
{
    // Mã định danh của user
    public int UserId { get; set; }

    // Địa chỉ email của user
    public string Email { get; set; } = string.Empty;

    // Họ tên đầy đủ
    public string? FullName { get; set; }

    // Số điện thoại của user
    public string? Phone { get; set; }

    // Thời điểm tạo tài khoản
    public DateTime CreateAt { get; set; }

    // Vai trò của user (admin, staff, customer) - trả về dạng chuỗi lowercase
    public string Role { get; set; } = string.Empty;

    // Trạng thái hoạt động của tài khoản
    public bool IsActive { get; set; }
}
