using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.DTOs.Auth;
using ServiceLayer.Exceptions;

namespace ControllerLayer.Controllers;

/// <summary>
/// Controller xử lý các yêu cầu HTTP liên quan đến xác thực (Đăng ký, Đăng nhập, Logout).
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    private readonly IAuthService _authService = authService;

    [AllowAnonymous]
    [HttpPost("register")]
    /// <summary>
    /// API Đăng ký tài khoản mới.
    /// </summary>
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    /// <summary>
    /// API Đăng nhập bằng Email và Password.
    /// </summary>
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    /// <summary>
    /// API Đăng nhập bằng Google.
    /// </summary>
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginWithGoogleAsync(request.Credential, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
        catch (Google.Apis.Auth.InvalidJwtException)
        {
            return Unauthorized(new { errorCode = "INVALID_CREDENTIALS", message = "Google credential is invalid." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-tokens")]
    /// <summary>
    /// API Làm mới Access Token khi hết hạn.
    /// </summary>
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokens([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshTokensAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize(Roles = "Admin,Staff,Customer")]
    [HttpPost("logout")]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        try
        {
            var result = await _authService.LogoutAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ApiException exception)
        {
            return ApiError(exception);
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new { errorCode = "UNAUTHORIZED", message = "Authentication required" });
        }

        return Ok(result);
    }
}
