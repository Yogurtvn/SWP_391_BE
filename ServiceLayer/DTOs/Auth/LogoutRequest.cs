using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Auth;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
