using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
