using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Entities;
using ServiceLayer.Contracts.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ServiceLayer.Services.Auth;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly IConfiguration _configuration = configuration;

    public string GenerateAccessToken(User user)
    {
        var issuer = GetRequiredConfigurationValue("Jwt:Issuer");
        var audience = GetRequiredConfigurationValue("Jwt:Audience");
        var key = GetRequiredConfigurationValue("Jwt:Key");
        var expiryInMinutes = GetExpiryInMinutes();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private double GetExpiryInMinutes()
    {
        var rawValue = GetRequiredConfigurationValue("Jwt:ExpiryInMinutes");

        if (!double.TryParse(rawValue, out var expiryInMinutes) || expiryInMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiry must be a positive number of minutes.");
        }

        return expiryInMinutes;
    }

    private string GetRequiredConfigurationValue(string key)
    {
        var value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration value '{key}' is missing or still using a placeholder.");
        }

        return value;
    }
}
