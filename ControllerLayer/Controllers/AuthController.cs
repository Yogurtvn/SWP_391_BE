using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Contracts.Auth;
using ServiceLayer.DTOs.Auth;
using System.Security.Claims;

namespace ControllerLayer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);

            if (result is null)
            {
                return Conflict(new { message = "Email is already registered." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);

            if (result is null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginWithGoogleAsync(request.Credential, cancellationToken);

            if (result is null)
            {
                return Unauthorized(new { message = "Google credential is invalid or the account cannot be linked." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, detail: exception.Message);
        }
        catch (Google.Apis.Auth.InvalidJwtException)
        {
            return Unauthorized(new { message = "Google credential is invalid." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User id claim is missing or invalid." });
        }

        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(result);
    }
}
