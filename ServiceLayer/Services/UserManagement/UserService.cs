using RepositoryLayer.Common;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Security;
using ServiceLayer.Contracts.UserManagement;
using ServiceLayer.DTOs.Common;
using ServiceLayer.DTOs.User.Request;
using ServiceLayer.DTOs.User.Response;
using ServiceLayer.Exceptions;
using System.Linq.Expressions;

namespace ServiceLayer.Services.UserManagement;

/// <summary>
/// Service xử lý toàn bộ nghiệp vụ liên quan đến User.
/// </summary>
public class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : IUserService
{
    // Inject UnitOfWork để truy cập repository và quản lý transaction
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Inject PasswordHasher để hash mật khẩu khi tạo user mới
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// Lấy profile của user hiện tại theo userId (từ JWT token).
    /// </summary>
    public async Task<UserProfileResponse> GetProfileAsync(int userId, CancellationToken cancellationToken)
    {
        // Tìm user trong database theo userId
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        // Nếu không tìm thấy, ném lỗi 404
        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        // Map entity sang response DTO và trả về
        return new UserProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone
        };
    }

    /// <summary>
    /// Cập nhật profile (fullName, phone) của user hiện tại.
    /// </summary>
    public async Task<MessageResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        // Tìm user trong database
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        // Nếu không tìm thấy, ném lỗi 404
        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        // Cập nhật các trường cho phép thay đổi
        user.FullName = request.FullName;
        user.Phone = request.Phone;

        // Đánh dấu entity đã thay đổi và lưu vào database
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Profile updated" };
    }

    /// <summary>
    /// Lấy danh sách users có phân trang, lọc, tìm kiếm, sắp xếp (chỉ Admin).
    /// </summary>
    public async Task<PagedResult<UserListItemResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken)
    {
        // Xây dựng bộ lọc dựa trên query parameters
        Expression<Func<User, bool>>? filter = BuildUserFilter(request);

        // Xây dựng hàm sắp xếp dựa trên sortBy và sortOrder
        Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = BuildUserOrderBy(request.SortBy, request.SortOrder);

        // Tạo PaginationRequest từ page và pageSize
        var paginationRequest = new PaginationRequest(request.Page, request.PageSize);

        // Gọi repository để lấy dữ liệu phân trang
        var pagedResult = await _unitOfWork.Repository<User>().GetPagedAsync(
            paginationRequest,
            filter: filter,
            orderBy: orderBy,
            tracked: false,
            cancellationToken: cancellationToken);

        // Map từng entity sang response DTO
        var items = pagedResult.Items.Select(u => new UserListItemResponse
        {
            UserId = u.UserId,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role.ToString().ToLower(), // Chuyển enum sang chuỗi lowercase ("admin", "staff", "customer")
            IsActive = u.IsActive
        }).ToList();

        // Trả về kết quả phân trang với dữ liệu đã map
        return PagedResult<UserListItemResponse>.Create(items, pagedResult.Page, pagedResult.PageSize, pagedResult.TotalItems);
    }

    /// <summary>
    /// Admin tạo tài khoản staff hoặc admin mới.
    /// </summary>
    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Parse role từ chuỗi sang enum, nếu không hợp lệ thì ném lỗi 400
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            throw new ApiException(400, "INVALID_ROLE", "Role must be 'admin', 'staff', or 'customer'");
        }

        // Kiểm tra email đã tồn tại chưa
        var emailExists = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.Email == request.Email);

        if (emailExists)
        {
            throw new ApiException(409, "EMAIL_ALREADY_EXISTS", "Email already exists");
        }

        // Tạo entity User mới với mật khẩu đã hash
        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password), // Hash mật khẩu trước khi lưu
            FullName = request.FullName,
            Phone = request.Phone,
            Role = role,
            CreatedAt = DateTime.UtcNow, // Ghi nhận thời gian tạo
            IsActive = true // Mặc định tài khoản mới được kích hoạt
        };

        // Lưu user vào database
        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Trả về thông tin user vừa tạo
        return new CreateUserResponse
        {
            UserId = user.UserId,
            Role = user.Role.ToString().ToLower()
        };
    }

    /// <summary>
    /// Admin khóa/mở khóa tài khoản user.
    /// </summary>
    public async Task<MessageResponse> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        // Tìm user theo userId
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        // Nếu không tìm thấy, ném lỗi 404
        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        // Cập nhật trạng thái hoạt động
        user.IsActive = request.IsActive;

        // Lưu thay đổi vào database
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "User status updated" };
    }

    /// <summary>
    /// Xây dựng biểu thức lọc user dựa trên các query parameters.
    /// </summary>
    private static Expression<Func<User, bool>>? BuildUserFilter(GetUsersRequest request)
    {
        // Bắt đầu với điều kiện luôn đúng (lấy tất cả)
        Expression<Func<User, bool>> filter = u => true;

        // Parse role nếu có
        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsedRole))
        {
            roleFilter = parsedRole;
        }

        // Chuẩn hóa từ khóa tìm kiếm
        var search = request.Search?.Trim().ToLower();

        // Kết hợp tất cả điều kiện lọc vào 1 expression
        // - Lọc theo role (nếu có)
        // - Lọc theo trạng thái isActive (nếu có)
        // - Tìm kiếm theo email hoặc fullName (nếu có)
        filter = u =>
            (roleFilter == null || u.Role == roleFilter) &&
            (request.IsActive == null || u.IsActive == request.IsActive) &&
            (string.IsNullOrEmpty(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.FullName != null && u.FullName.ToLower().Contains(search)));

        return filter;
    }

    /// <summary>
    /// Xây dựng hàm sắp xếp dựa trên sortBy và sortOrder.
    /// </summary>
    private static Func<IQueryable<User>, IOrderedQueryable<User>>? BuildUserOrderBy(string? sortBy, string? sortOrder)
    {
        // Xác định chiều sắp xếp: mặc định là giảm dần (desc)
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        // Áp dụng sắp xếp theo trường được chỉ định
        return sortBy?.ToLower() switch
        {
            "email" => isDescending
                ? q => q.OrderByDescending(u => u.Email)
                : q => q.OrderBy(u => u.Email),
            "fullname" => isDescending
                ? q => q.OrderByDescending(u => u.FullName)
                : q => q.OrderBy(u => u.FullName),
            _ => isDescending // Mặc định sắp xếp theo CreatedAt
                ? q => q.OrderByDescending(u => u.CreatedAt)
                : q => q.OrderBy(u => u.CreatedAt)
        };
    }
}
