using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.Contracts.Security;
using ServiceLayer.DTOs.Auth;

namespace ServiceLayer.Services.Auth;

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

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var normalizedEmail = NormalizeEmail(request.Email);

        var existingUser = await userRepository.GetFirstOrDefaultAsync(
            user => user.Email == normalizedEmail,
            tracked: false);

        if (existingUser is not null)
        {
            return null;
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

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.Email == normalizedEmail);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default)
    {
        var googleClientId = GetRequiredConfigurationValue("GoogleAuth:ClientId");
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { googleClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(credential.Trim().Trim('"'), validationSettings);

        if (string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
        {
            return null;
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
            return null;
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
            return null;
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<User>();

        var user = await userRepository.GetFirstOrDefaultAsync(
            currentUser => currentUser.UserId == userId,
            tracked: false);

        if (user is null)
        {
            return null;
        }

        return MapUser(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return new AuthResponse
        {
            AccessToken = _tokenService.GenerateAccessToken(user),
            User = MapUser(user)
        };
    }

    private static AuthUserResponse MapUser(User user)
    {
        return new AuthUserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            HasGoogleLogin = !string.IsNullOrWhiteSpace(user.GoogleSubjectId)
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

    private string GetRequiredConfigurationValue(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration value '{key}' is missing or still using a placeholder.");
        }

        return value;
    }
}
