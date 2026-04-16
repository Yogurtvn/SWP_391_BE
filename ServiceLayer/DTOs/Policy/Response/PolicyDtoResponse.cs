namespace ServiceLayer.DTOs.Policy.Response;

// DTO trả về thông tin chi tiết của 1 policy (dùng cho GET /api/Policies/{policyId})
public class PolicyDtoResponse
{
    public int PolicyId { get; set; }          // ID của policy

    public string Title { get; set; } = string.Empty;    // Tiêu đề policy

    public string Content { get; set; } = string.Empty;  // Nội dung policy
}
