using ServiceLayer.DTOs.Auth;

namespace ServiceLayer.Contracts.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);

    Task<RefreshTokenResponse> RefreshTokensAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<LogoutResponse> LogoutAsync(int userId, LogoutRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
