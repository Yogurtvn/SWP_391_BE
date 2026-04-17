namespace ServiceLayer.DTOs.Category.Request;

/// <summary>
/// DTO nhận query parameters cho API lấy danh sách categories.
/// Hỗ trợ phân trang, tìm kiếm và sắp xếp.
/// </summary>
public class GetCategoriesRequest
{
    // Trang hiện tại (mặc định = 1)
    public int Page { get; set; } = 1;

    // Số lượng item mỗi trang (mặc định = 20)
    public int PageSize { get; set; } = 20;

    // Từ khóa tìm kiếm theo tên category (không bắt buộc)
    public string? Search { get; set; }

    // Trường dùng để sắp xếp: "categoryName", "categoryId" (mặc định = "categoryId")
    public string? SortBy { get; set; }

    // Thứ tự sắp xếp: "asc" hoặc "desc" (mặc định = "asc")
    public string? SortOrder { get; set; }
}
