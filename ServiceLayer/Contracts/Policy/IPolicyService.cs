using RepositoryLayer.Common;
using ServiceLayer.DTOs.Policy.Request;
using ServiceLayer.DTOs.Policy.Response;

namespace ServiceLayer.Contracts.Policy;

// Interface định nghĩa các nghiệp vụ CRUD cho Policy
public interface IPolicyService
{
    // Lấy danh sách policy có phân trang và tìm kiếm theo title
    Task<PagedResult<PolicyDtoResponse>> GetPoliciesAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken = default);

    // Lấy chi tiết 1 policy theo ID, trả về null nếu không tìm thấy
    Task<PolicyDtoResponse?> GetPolicyByIdAsync(int policyId, CancellationToken cancellationToken = default);

    // Tạo policy mới, trả về PolicyId vừa tạo
    Task<int> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);

    // Cập nhật policy, trả về true nếu thành công, false nếu không tìm thấy
    Task<bool> UpdatePolicyAsync(int policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default);

    // Xóa policy, trả về true nếu thành công, false nếu không tìm thấy
    Task<bool> DeletePolicyAsync(int policyId, CancellationToken cancellationToken = default);
}
