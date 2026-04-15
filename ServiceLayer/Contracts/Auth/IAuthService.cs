using ServiceLayer.DTOs.Auth;

namespace ServiceLayer.Contracts.Auth;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);

    Task<AuthUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
