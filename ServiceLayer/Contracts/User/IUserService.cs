using RepositoryLayer.Common;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.User.Request;
using ServiceLayer.DTOs.User.Response;

namespace ServiceLayer.Contracts.UserManagement;

/// <summary>
/// Interface định nghĩa các nghiệp vụ liên quan đến User.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lấy thông tin profile của user hiện tại theo userId từ token.
    /// </summary>
    Task<UserProfileResponse> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật profile (fullName, phone) của user hiện tại.
    /// </summary>
    Task<MessageResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách users có phân trang, lọc, tìm kiếm, sắp xếp (Admin only).
    /// </summary>
    Task<PagedResult<UserListItemResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin tạo tài khoản staff hoặc admin mới.
    /// </summary>
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin khóa/mở khóa tài khoản user.
    /// </summary>
    Task<MessageResponse> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}
