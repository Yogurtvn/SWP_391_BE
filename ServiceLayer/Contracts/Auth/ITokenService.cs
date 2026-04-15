using RepositoryLayer.Entities;

namespace ServiceLayer.Contracts.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}
