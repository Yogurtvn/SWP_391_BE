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
/// Dịch vụ xử lý liên quan đến JWT Token (JSON Web Token).
/// JWT gồm 3 phần: Header (Thuật toán), Payload (Dữ liệu/Claims), Signature (Chữ ký bảo mật).
/// </summary>
public class TokenService(IConfiguration configuration) : ITokenService
{
    private const string AccessTokenType = "access"; // Token dùng để truy cập API (thời hạn ngắn)
    private const string RefreshTokenType = "refresh"; // Token dùng để lấy Access Token mới (thời hạn dài)

    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Tạo Access Token chứa các thông tin định danh của người dùng (Claims).
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // Claims là các mẩu thông tin về người dùng được nhúng vào trong Token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()), // ID người dùng
            new(ClaimTypes.Email, user.Email), // Email
            new(ClaimTypes.Name, user.FullName ?? string.Empty), // Tên đầy đủ
            new(ClaimTypes.Role, user.Role.ToString()), // Quyền hạn (Admin/Staff/Customer)
            new(TokenClaimNames.TokenType, AccessTokenType), // Phân loại token
            new(TokenClaimNames.TokenVersion, user.TokenVersion.ToString(CultureInfo.InvariantCulture)) // Phiên bản token (dùng để thu hồi token khi đổi pass)
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

    /// <summary>
    /// Giải mã và kiểm tra tính hợp lệ của Refresh Token.
    /// Nếu token hợp lệ, trả về đối tượng Principal chứa các Claims.
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
            // Thực hiện validate chữ ký, nhà phát hành, khán giả và thời gian hết hạn
            var principal = tokenHandler.ValidateToken(
                refreshToken.Trim(),
                BuildTokenValidationParameters(validateLifetime: true),
                out _);

            // Kiểm tra xem đây có đúng là loại Refresh Token không
            var tokenType = principal.FindFirst(TokenClaimNames.TokenType)?.Value;
            return string.Equals(tokenType, RefreshTokenType, StringComparison.Ordinal)
                ? principal
                : null;
        }
        catch
        {
            // Nếu token sai chữ ký hoặc hết hạn, ValidateToken sẽ ném lỗi và ta trả về null
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
