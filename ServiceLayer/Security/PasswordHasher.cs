using ServiceLayer.Contracts.Security;

namespace ServiceLayer.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
    }

    public bool Verify(string plainTextPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
    }
}
