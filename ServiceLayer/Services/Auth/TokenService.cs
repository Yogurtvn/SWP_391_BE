using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Entities;
using ServiceLayer.Contracts.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ServiceLayer.Services.Auth;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";

    private readonly IConfiguration _configuration = configuration;

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(TokenClaimNames.TokenType, AccessTokenType),
            new(TokenClaimNames.TokenVersion, user.TokenVersion.ToString(CultureInfo.InvariantCulture))
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(GetAccessExpiryInMinutes()));
    }

    public string GenerateRefreshToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(TokenClaimNames.TokenType, RefreshTokenType),
            new(TokenClaimNames.TokenVersion, user.TokenVersion.ToString(CultureInfo.InvariantCulture))
        };

        return GenerateToken(claims, DateTime.UtcNow.AddDays(GetRefreshExpiryInDays()));
    }

    public ClaimsPrincipal? GetPrincipalFromRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(
                refreshToken.Trim(),
                BuildTokenValidationParameters(validateLifetime: true),
                out _);

            var tokenType = principal.FindFirst(TokenClaimNames.TokenType)?.Value;
            return string.Equals(tokenType, RefreshTokenType, StringComparison.Ordinal)
                ? principal
                : null;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims, DateTime expiresAtUtc)
    {
        var issuer = GetRequiredConfigurationValue("Jwt:Issuer");
        var audience = GetRequiredConfigurationValue("Jwt:Audience");
        var key = GetRequiredConfigurationValue("Jwt:Key");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters BuildTokenValidationParameters(bool validateLifetime)
    {
        var issuer = GetRequiredConfigurationValue("Jwt:Issuer");
        var audience = GetRequiredConfigurationValue("Jwt:Audience");
        var key = GetRequiredConfigurationValue("Jwt:Key");

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.Zero
        };
    }

    private double GetAccessExpiryInMinutes()
    {
        var rawValue = GetRequiredConfigurationValue("Jwt:ExpiryInMinutes");

        if (!double.TryParse(rawValue, out var expiryInMinutes) || expiryInMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiry must be a positive number of minutes.");
        }

        return expiryInMinutes;
    }

    private double GetRefreshExpiryInDays()
    {
        var rawValue = _configuration["Jwt:RefreshTokenExpiryInDays"];

        if (double.TryParse(rawValue, out var expiryInDays) && expiryInDays > 0)
        {
            return expiryInDays;
        }

        // TODO: replace this fallback once refresh token lifecycle is finalized in API_SPEC.md.
        return 7;
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
