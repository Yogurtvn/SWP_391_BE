namespace ServiceLayer.DTOs.Policy.Response;

// DTO trả về danh sách policy có phân trang (dùng cho GET /api/Policies)
public class PolicyListResponse
{
    public IEnumerable<PolicyDtoResponse> Items { get; set; } = [];  // Danh sách policy trong trang hiện tại

    public int Page { get; set; }        // Trang hiện tại (bắt đầu từ 1)

    public int PageSize { get; set; }    // Số lượng item mỗi trang

    public int TotalItems { get; set; }  // Tổng số policy trong DB (sau khi lọc search)

    public int TotalPages { get; set; }  // Tổng số trang = ceil(TotalItems / PageSize)
}
