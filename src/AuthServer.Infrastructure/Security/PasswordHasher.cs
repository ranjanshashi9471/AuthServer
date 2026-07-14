using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> Hasher = new();

    public PasswordHash Hash(string password)
    {
        var hash = Hasher.HashPassword(new object(), password);

        return PasswordHash.From(hash);
    }

    public bool Verify(
        string password,
        PasswordHash passwordHash)
    {
        var result = Hasher.VerifyHashedPassword(
            new object(),
            passwordHash.Value,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}