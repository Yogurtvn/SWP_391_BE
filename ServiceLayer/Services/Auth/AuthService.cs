using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Security;
using ServiceLayer.DTOs.Auth;
using ServiceLayer.Exceptions;
using System.Globalization;
using System.Net;
using System.Security.Claims;

namespace ServiceLayer.Services.Auth;

/// <summary>
/// Dịch vụ quản lý các tính năng xác thực như Đăng ký, Đăng nhập, Đăng nhập Google, và Đăng xuất.
/// </summary>
public class AuthService(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IConfiguration configuration) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Đăng ký tài khoản người dùng mới.
    /// </summary>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var normalizedEmail = NormalizeEmail(request.Email);

        var existingUser = await userRepository.GetFirstOrDefaultAsync(
            user => user.Email == normalizedEmail,
            tracked: false);

        if (existingUser is not null)
        {
            throw new ApiException((int)HttpStatusCode.Conflict, "EMAIL_ALREADY_EXISTS", "Email already exists");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = NormalizePhone(request.Phone),
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Role = ToApiEnum(user.Role)
        };
    }

    /// <summary>
    /// Đăng nhập bằng Email và Mật khẩu.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.Email == normalizedEmail);

        if (user is null || !user.IsActive)
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Invalid email or password");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Invalid email or password");
        }

        return BuildAuthResponse(user);
    }

    /// <summary>
    /// Đăng nhập thông qua tài khoản Google.
    /// </summary>
    public async Task<AuthResponse> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default)
    {
        var googleClientId = GetRequiredConfigurationValue("GoogleAuth:ClientId");
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { googleClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(credential.Trim().Trim('"'), validationSettings);

        if (string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Google credential is invalid or the account cannot be linked.");
        }

        var userRepository = _unitOfWork.Repository<User>();
        var normalizedEmail = NormalizeEmail(payload.Email);

        var user = await userRepository.GetFirstOrDefaultAsync(currentUser => currentUser.GoogleSubjectId == payload.Subject);

        if (user is null)
        {
            user = await userRepository.GetFirstOrDefaultAsync(currentUser => currentUser.Email == normalizedEmail);
        }

        if (user is null)
        {
            user = new User
            {
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
                FullName = payload.Name?.Trim(),
                Phone = null,
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                GoogleSubjectId = payload.Subject
            };

            await userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BuildAuthResponse(user);
        }

        if (!user.IsActive)
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Google credential is invalid or the account cannot be linked.");
        }

        if (string.IsNullOrWhiteSpace(user.GoogleSubjectId))
        {
            user.GoogleSubjectId = payload.Subject;
            user.FullName ??= payload.Name?.Trim();
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (!string.Equals(user.GoogleSubjectId, payload.Subject, StringComparison.Ordinal))
        {
            throw new ApiException((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Google credential is invalid or the account cannot be linked.");
        }

        return BuildAuthResponse(user);
    }

    /// <summary>
    /// Làm mới Access Token bằng Refresh Token.
    /// </summary>
    public async Task<RefreshTokenResponse> RefreshTokensAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.GetPrincipalFromRefreshToken(request.RefreshToken);

        if (principal is null
            || !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            || !TryGetTokenVersion(principal, out var tokenVersion))
        {
            throw new ApiException(
                (int)HttpStatusCode.Unauthorized,
                "INVALID_REFRESH_TOKEN",
                "Refresh token is invalid or expired");
        }

        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.UserId == userId && currentUser.IsActive,
            tracked: false);

        if (user is null)
        {
            throw new ApiException(
                (int)HttpStatusCode.Unauthorized,
                "INVALID_REFRESH_TOKEN",
                "Refresh token is invalid or expired");
        }

        if (user.TokenVersion != tokenVersion)
        {
            throw new ApiException(
                (int)HttpStatusCode.Unauthorized,
                "INVALID_REFRESH_TOKEN",
                "Refresh token is invalid or expired");
        }

        return new RefreshTokenResponse
        {
            AccessToken = _tokenService.GenerateAccessToken(user)
        };
    }

    /// <summary>
    /// Đăng xuất người dùng bằng cách vô hiệu hóa Refresh Token (tăng TokenVersion).
    /// </summary>
    public async Task<LogoutResponse> LogoutAsync(int userId, LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.GetPrincipalFromRefreshToken(request.RefreshToken);

        if (principal is null
            || !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var refreshTokenUserId)
            || !TryGetTokenVersion(principal, out var tokenVersion)
            || refreshTokenUserId != userId)
        {
            throw new ApiException((int)HttpStatusCode.BadRequest, "LOGOUT_FAILED", "Logout failed");
        }

        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.UserId == userId && currentUser.IsActive,
            tracked: true);

        if (user is null || user.TokenVersion != tokenVersion)
        {
            throw new ApiException((int)HttpStatusCode.BadRequest, "LOGOUT_FAILED", "Logout failed");
        }

        user.TokenVersion++; // Tăng version để làm các token cũ không còn hợp lệ
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LogoutResponse
        {
            Message = "Logged out successfully"
        };
    }

    /// <summary>
    /// Lấy thông tin chi tiết của người dùng đang đăng nhập.
    /// </summary>
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();

        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.UserId == userId && currentUser.IsActive,
            tracked: false);

        if (user is null)
        {
            return null;
        }

        return new CurrentUserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Role = ToApiEnum(user.Role)
        };
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return new AuthResponse
        {
            AccessToken = _tokenService.GenerateAccessToken(user),
            RefreshToken = _tokenService.GenerateRefreshToken(user),
            User = MapUser(user)
        };
    }

    private static AuthUserResponse MapUser(User user)
    {
        return new AuthUserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Role = ToApiEnum(user.Role)
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phone)
    {
        var normalizedPhone = phone?.Trim();
        return string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone;
    }

    private static string ToApiEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return value.ToString().ToLowerInvariant();
    }

    private string GetRequiredConfigurationValue(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration value '{key}' is missing or still using a placeholder.");
        }

        return value;
    }

    private static bool TryGetTokenVersion(ClaimsPrincipal principal, out int tokenVersion)
    {
        var rawTokenVersion = principal.FindFirst(TokenClaimNames.TokenVersion)?.Value;

        if (string.IsNullOrWhiteSpace(rawTokenVersion))
        {
            tokenVersion = 0;
            return true;
        }

        return int.TryParse(rawTokenVersion, NumberStyles.None, CultureInfo.InvariantCulture, out tokenVersion)
            && tokenVersion >= 0;
    }
}
