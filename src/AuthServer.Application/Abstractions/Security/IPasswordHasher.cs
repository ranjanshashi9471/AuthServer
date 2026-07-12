using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Abstractions.Security;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);

    bool Verify(
        string password,
        PasswordHash passwordHash);
}