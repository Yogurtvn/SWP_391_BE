using System.Security.Claims;
using RepositoryLayer.Entities;

namespace ServiceLayer.Contracts.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken(User user);

    ClaimsPrincipal? GetPrincipalFromRefreshToken(string refreshToken);
}
