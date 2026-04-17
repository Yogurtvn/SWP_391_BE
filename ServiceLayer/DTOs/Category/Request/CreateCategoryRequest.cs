using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Category.Request;

/// <summary>
/// DTO nhận dữ liệu khi Admin tạo category mới.
/// </summary>
public class CreateCategoryRequest
{
    // Tên category - bắt buộc, phải duy nhất trong hệ thống
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name must not exceed 100 characters")]
    public string CategoryName { get; set; } = string.Empty;
}
