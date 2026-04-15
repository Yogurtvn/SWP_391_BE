using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.DTOs.Auth;

public class GoogleLoginRequest
{
    [Required]
    public string Credential { get; set; } = string.Empty;
}
