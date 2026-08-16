using AuthServer.Domain.Common;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class EmailVerificationToken : Entity<EmailVerificationTokenId>
{
    public UserId UserId { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? InvalidatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private EmailVerificationToken() { }

    private EmailVerificationToken(
        EmailVerificationTokenId id,
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

    public static EmailVerificationToken Create(
        EmailVerificationTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt
    )
    {
        return new EmailVerificationToken(id, userId, tokenHash, expiresAt);
    }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt is not null;
    public bool IsInvalidated => InvalidatedAt is not null;
    public bool IsActive => !IsExpired && !IsUsed && !IsInvalidated;

    public void Use()
    {
        if (IsUsed)
        {
            throw new BusinessRuleViolationException(
                "Email verification token has already been used."
            );
        }

        UsedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Invalidate()
    {
        if (IsInvalidated || IsUsed)
        {
            return;
        }

        InvalidatedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
