namespace ServiceLayer.DTOs.Auth;

public class AuthUserResponse
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool HasGoogleLogin { get; set; }
}
