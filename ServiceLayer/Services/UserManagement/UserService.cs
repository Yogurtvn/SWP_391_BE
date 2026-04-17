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
using System.Net;

namespace ServiceLayer.Services.UserManagement;

/// <summary>
/// Handles user profile, admin user management, and account activation state.
/// </summary>
public class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// Returns the current user's profile.
    /// </summary>
    public async Task<UserProfileResponse> GetProfileAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        if (!user.IsActive)
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Authentication required");
        }

        return new UserProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone
        };
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    public async Task<MessageResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        if (!user.IsActive)
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Authentication required");
        }

        user.FullName = request.FullName;
        user.Phone = request.Phone;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "Profile updated" };
    }

    /// <summary>
    /// Returns the paged user list for admin screens.
    /// </summary>
    public async Task<PagedResult<UserListItemResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken)
    {
        Expression<Func<User, bool>>? filter = BuildUserFilter(request);
        Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = BuildUserOrderBy(request.SortBy, request.SortOrder);
        var paginationRequest = new PaginationRequest(request.Page, request.PageSize);

        var pagedResult = await _unitOfWork.Repository<User>().GetPagedAsync(
            paginationRequest,
            filter: filter,
            orderBy: orderBy,
            tracked: false,
            cancellationToken: cancellationToken);

        var items = pagedResult.Items.Select(u => new UserListItemResponse
        {
            UserId = u.UserId,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role.ToString().ToLower(),
            IsActive = u.IsActive
        }).ToList();

        return PagedResult<UserListItemResponse>.Create(items, pagedResult.Page, pagedResult.PageSize, pagedResult.TotalItems);
    }

    /// <summary>
    /// Allows admins to create staff or admin accounts.
    /// </summary>
    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role)
            || role is not UserRole.Admin and not UserRole.Staff)
        {
            throw new ApiException(400, "INVALID_ROLE", "Role must be 'admin' or 'staff'");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.Email == normalizedEmail);

        if (emailExists)
        {
            throw new ApiException(409, "EMAIL_ALREADY_EXISTS", "Email already exists");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse
        {
            UserId = user.UserId,
            Role = user.Role.ToString().ToLower()
        };
    }

    /// <summary>
    /// Allows admins to activate or deactivate an account.
    /// </summary>
    public async Task<MessageResponse> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);

        if (user is null)
        {
            throw new ApiException(404, "USER_NOT_FOUND", "User not found");
        }

        var isBeingDeactivated = user.IsActive && !request.IsActive;
        user.IsActive = request.IsActive;

        if (isBeingDeactivated)
        {
            user.TokenVersion++;
        }

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MessageResponse { Message = "User status updated" };
    }

    private static Expression<Func<User, bool>>? BuildUserFilter(GetUsersRequest request)
    {
        Expression<Func<User, bool>> filter = u => true;

        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Role)
            && Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsedRole))
        {
            roleFilter = parsedRole;
        }

        var search = request.Search?.Trim().ToLower();

        filter = u =>
            (roleFilter == null || u.Role == roleFilter) &&
            (request.IsActive == null || u.IsActive == request.IsActive) &&
            (string.IsNullOrEmpty(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.FullName != null && u.FullName.ToLower().Contains(search)));

        return filter;
    }

    private static Func<IQueryable<User>, IOrderedQueryable<User>>? BuildUserOrderBy(string? sortBy, string? sortOrder)
    {
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLower() switch
        {
            "email" => isDescending
                ? q => q.OrderByDescending(u => u.Email)
                : q => q.OrderBy(u => u.Email),
            "fullname" => isDescending
                ? q => q.OrderByDescending(u => u.FullName)
                : q => q.OrderBy(u => u.FullName),
            _ => isDescending
                ? q => q.OrderByDescending(u => u.CreatedAt)
                : q => q.OrderBy(u => u.CreatedAt)
        };
    }
}
