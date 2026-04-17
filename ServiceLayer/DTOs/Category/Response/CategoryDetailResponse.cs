namespace ServiceLayer.DTOs.Category.Response;

/// <summary>
/// DTO trả về chi tiết category (dùng cho cả danh sách và chi tiết).
/// </summary>
public class CategoryDetailResponse
{
    // Mã định danh của category
    public int CategoryId { get; set; }

    // Tên category
    public string CategoryName { get; set; } = string.Empty;
}
