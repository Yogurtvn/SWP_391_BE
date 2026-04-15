namespace ServiceLayer.DTOs.Auth;

public class AuthResponse
{
    public string TokenType { get; set; } = "Bearer";

    public string AccessToken { get; set; } = string.Empty;

    public AuthUserResponse User { get; set; } = new();
}
