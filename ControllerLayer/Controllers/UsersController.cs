using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Common;
using ServiceLayer.Contracts.UserManagement;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.User.Request;
using ServiceLayer.DTOs.User.Response;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller xử lý tất cả API liên quan đến User:
/// - Get/Update profile của user hiện tại
/// - Admin: CRUD users, khóa/mở khóa tài khoản
/// </summary>
[Route("api/users")]
[ApiController]
public class UsersController(IUserService userService) : ApiControllerBase
{
    // Inject UserService để gọi các nghiệp vụ user
    private readonly IUserService _userService = userService;

    /// <summary>
    /// GET /api/users/profile
    /// Lấy thông tin profile của user đang đăng nhập.
    /// Yêu cầu: đã xác thực (Customer, Staff, Admin).
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        try
        {
            // Lấy userId từ JWT token của user hiện tại
            if (!TryGetCurrentUserId(out var userId))
            {
                // Nếu không lấy được userId (token không hợp lệ), trả về 401
                return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
            }

            // Gọi service để lấy profile
            var result = await _userService.GetProfileAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            // Xử lý lỗi nghiệp vụ (ví dụ: user not found)
            return ApiError(exception);
        }
    }

    /// <summary>
    /// PUT /api/users/profile
    /// Cập nhật thông tin profile (fullName, phone) của user đang đăng nhập.
    /// Yêu cầu: đã xác thực (Customer, Staff, Admin).
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<MessageResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Lấy userId từ JWT token
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
            }

            // Gọi service để cập nhật profile
            var result = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// GET /api/users
    /// Lấy danh sách users có phân trang, lọc, tìm kiếm, sắp xếp.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để lấy danh sách users phân trang
            var result = await _userService.GetUsersAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// POST /api/users
    /// Admin tạo tài khoản staff hoặc admin mới.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để tạo user mới
            var result = await _userService.CreateUserAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    /// <summary>
    /// PATCH /api/users/{userId}/status
    /// Admin khóa hoặc mở khóa tài khoản user.
    /// Yêu cầu: Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{userId:int}/status")]
    public async Task<ActionResult<MessageResponse>> UpdateUserStatus(
        int userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Gọi service để cập nhật trạng thái user
            var result = await _userService.UpdateUserStatusAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }
}
