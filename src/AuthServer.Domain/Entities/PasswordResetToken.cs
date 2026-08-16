using AuthServer.Domain.Common;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class PasswordResetToken : Entity<PasswordResetTokenId>
{
    public UserId UserId { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public User User { get; private set; } = null!;

    private PasswordResetToken() { }

    private PasswordResetToken(
        PasswordResetTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt
    )
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static PasswordResetToken Create(
        PasswordResetTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt
    )
    {
        return new PasswordResetToken(id, userId, tokenHash, expiresAt);
    }

    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;

    public bool IsUsed => UsedAt is not null;

    public void Use()
    {
        if (IsUsed)
        {
            throw new BusinessRuleViolationException("Password reset token has already been used.");
        }

        if (IsExpired)
        {
            throw new BusinessRuleViolationException("Password reset token has expired.");
        }

        UsedAt = DateTimeOffset.UtcNow;

        Touch();
    }
}
