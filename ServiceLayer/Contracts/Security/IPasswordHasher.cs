namespace ServiceLayer.Contracts.Security;

public interface IPasswordHasher
{
    string Hash(string plainTextPassword);

    bool Verify(string plainTextPassword, string hashedPassword);
}
