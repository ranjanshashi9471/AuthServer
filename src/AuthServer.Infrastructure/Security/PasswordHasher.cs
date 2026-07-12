using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public PasswordHash Hash(string password)
    {
        var hash = _hasher.HashPassword(new object(), password);

        return PasswordHash.From(hash);
    }

    public bool Verify(
        string password,
        PasswordHash passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(
            new object(),
            passwordHash.Value,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}