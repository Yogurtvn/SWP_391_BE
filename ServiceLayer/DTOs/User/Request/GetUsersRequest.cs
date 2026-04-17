namespace ServiceLayer.DTOs.User.Request;

/// <summary>
/// DTO nhận query parameters cho API lấy danh sách users (Admin).
/// Hỗ trợ phân trang, lọc theo role/trạng thái, tìm kiếm và sắp xếp.
/// </summary>
public class GetUsersRequest
{
    // Trang hiện tại (mặc định = 1)
    public int Page { get; set; } = 1;

    // Số lượng item mỗi trang (mặc định = 20)
    public int PageSize { get; set; } = 20;

    // Lọc theo vai trò: "admin", "staff", "customer" (không bắt buộc)
    public string? Role { get; set; }

    // Lọc theo trạng thái hoạt động (không bắt buộc)
    public bool? IsActive { get; set; }

    // Từ khóa tìm kiếm theo email hoặc tên (không bắt buộc)
    public string? Search { get; set; }

    // Trường dùng để sắp xếp: "email", "fullName", "createdAt" (mặc định = "createdAt")
    public string? SortBy { get; set; }

    // Thứ tự sắp xếp: "asc" hoặc "desc" (mặc định = "desc")
    public string? SortOrder { get; set; }
}
