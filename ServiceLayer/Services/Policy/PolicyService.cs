using RepositoryLayer.Common;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Policy;
using ServiceLayer.DTOs.Policy.Request;
using ServiceLayer.DTOs.Policy.Response;
using PolicyEntity = RepositoryLayer.Entities.Policy; // Alias để tránh xung đột namespace với folder Policy

namespace ServiceLayer.Services.Policy;

// Service xử lý logic nghiệp vụ CRUD cho Policy, sử dụng UnitOfWork + GenericRepository
public class PolicyService(IUnitOfWork unitOfWork) : IPolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork; // Inject UnitOfWork để truy cập repository và quản lý transaction

    public async Task<PagedResult<PolicyDtoResponse>> GetPoliciesAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var repository = _unitOfWork.Repository<PolicyEntity>();
        var normalizedSearch = NormalizeSearch(search);
        var pagedPolicies = await repository.GetPagedAsync(
            paginationRequest: paginationRequest,
            filter: normalizedSearch is null
                ? null
                : policy => policy.Title.Contains(normalizedSearch),
            orderBy: query => query.OrderByDescending(policy => policy.CreatedAt),
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedPolicies.Items
            .Select(MapToDto)
            .ToList();

        return PagedResult<PolicyDtoResponse>.Create(
            items,
            pagedPolicies.Page,
            pagedPolicies.PageSize,
            pagedPolicies.TotalItems);
    }

    public async Task<PolicyDtoResponse?> GetPolicyByIdAsync(int policyId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PolicyEntity>();

        var policy = await repository.GetByIdAsync(policyId); // Tìm policy theo primary key

        return policy is null ? null : MapToDto(policy);      // Trả về null nếu không tìm thấy, hoặc DTO nếu có
    }

    public async Task<int> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PolicyEntity>();

        // Tạo entity mới từ request DTO
        var policy = new PolicyEntity
        {
            Title = request.Title.Trim(),         // Trim khoảng trắng đầu/cuối
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow,          // Gán thời gian tạo
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(policy);                       // Thêm vào DbContext (chưa lưu DB)
        await _unitOfWork.SaveChangesAsync(cancellationToken);   // Lưu vào DB → PolicyId được tự động gán bởi EF Core

        return policy.PolicyId;                                  // Trả về ID vừa được DB generate
    }

    public async Task<bool> UpdatePolicyAsync(int policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PolicyEntity>();

        var policy = await repository.GetByIdAsync(policyId);    // Tìm policy cần update (tracked)

        if (policy is null)
        {
            return false;                                        // Không tìm thấy → trả về false
        }

        // Cập nhật các field từ request
        policy.Title = request.Title.Trim();
        policy.Content = request.Content.Trim();
        policy.UpdatedAt = DateTime.UtcNow;                      // Cập nhật thời gian sửa

        repository.Update(policy);                               // Đánh dấu entity đã thay đổi
        await _unitOfWork.SaveChangesAsync(cancellationToken);   // Lưu thay đổi vào DB

        return true;                                             // Cập nhật thành công
    }

    public async Task<bool> DeletePolicyAsync(int policyId, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PolicyEntity>();

        var policy = await repository.GetByIdAsync(policyId);    // Tìm policy cần xóa

        if (policy is null)
        {
            return false;                                        // Không tìm thấy → trả về false
        }

        repository.Remove(policy);                               // Đánh dấu entity cần xóa
        await _unitOfWork.SaveChangesAsync(cancellationToken);   // Thực hiện DELETE trong DB

        return true;                                             // Xóa thành công
    }

    // Helper method: chuyển đổi từ Entity sang DTO để trả về cho client
    private static PolicyDtoResponse MapToDto(PolicyEntity policy)
    {
        return new PolicyDtoResponse
        {
            PolicyId = policy.PolicyId,
            Title = policy.Title,
            Content = policy.Content
        };
    }

    private static string? NormalizeSearch(string? search)
    {
        var normalizedSearch = search?.Trim();
        return string.IsNullOrWhiteSpace(normalizedSearch) ? null : normalizedSearch;
    }
}
