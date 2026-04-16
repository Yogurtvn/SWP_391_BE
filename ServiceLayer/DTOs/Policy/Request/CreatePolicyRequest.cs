using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Policy.Request;

// DTO dùng cho request body khi tạo policy mới (POST /api/Policies)
public class CreatePolicyRequest
{
    [Required]       // Bắt buộc phải có giá trị
    [MaxLength(255)] // Giới hạn tối đa 255 ký tự (khớp với DB constraint)
    public string Title { get; set; } = string.Empty;

    [Required]       // Bắt buộc phải có giá trị
    public string Content { get; set; } = string.Empty;
}
