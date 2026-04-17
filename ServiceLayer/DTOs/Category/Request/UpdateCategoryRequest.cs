using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Category.Request;

/// <summary>
/// DTO nhận dữ liệu khi Admin cập nhật category.
/// </summary>
public class UpdateCategoryRequest
{
    // Tên category mới - bắt buộc
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name must not exceed 100 characters")]
    public string CategoryName { get; set; } = string.Empty;
}
