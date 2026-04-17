namespace ServiceLayer.DTOs.Auth;

public class CurrentUserResponse
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string Role { get; set; } = string.Empty;
}
