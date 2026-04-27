using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer.Entities;
using ServiceLayer.Contracts.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ServiceLayer.Services.Auth;

/// <summary>
/// Dịch vụ xử lý liên quan đến JWT Token (Access Token và Refresh Token).
/// </summary>
public class TokenService(IConfiguration configuration) : ITokenService
{
    private const string AccessTokenType = "access"; // Định nghĩa loại token truy cập
    private const string RefreshTokenType = "refresh"; // Định nghĩa loại token làm mới (refresh)

    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Tạo Access Token cho người dùng dựa trên thông tin User.
    /// </summary>
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

        // Trả về token đã được ký với thời gian hết hạn lấy từ cấu hình
        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(GetAccessExpiryInMinutes()));
    }

    /// <summary>
    /// Tạo Refresh Token để người dùng có thể lấy Access Token mới mà không cần đăng nhập lại.
    /// </summary>
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

    /// <summary>
    /// Giải mã và kiểm tra tính hợp lệ của Refresh Token để lấy thông tin người dùng (Principal).
    /// </summary>
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

    /// <summary>
    /// Hàm dùng chung để tạo chuỗi JWT Token từ danh sách Claims và thời gian hết hạn.
    /// </summary>
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

        // Chuyển đối tượng token thành chuỗi ký tự JWT
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

    /// <summary>
    /// Lấy giá trị cấu hình bắt buộc từ file appsettings.json, ném lỗi nếu thiếu.
    /// </summary>
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
