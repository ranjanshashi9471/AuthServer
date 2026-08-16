using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;

namespace AuthServer.Infrastructure.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly IdentityPasswordHasher Hasher = new();

    public PasswordHash Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var hash = Hasher.HashPassword(new object(), password);

        return PasswordHash.From(hash);
    }

    public bool Verify(string password, PasswordHash passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var result = Hasher.VerifyHashedPassword(new object(), passwordHash.Value, password);

        return result != PasswordVerificationResult.Failed;
    }
}
