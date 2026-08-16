using AuthServer.Domain.Common;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class RefreshToken : Entity<RefreshTokenId>
{
    public UserId UserId { get; private set; } = null!;

    public RefreshTokenFamilyId FamilyId { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken() { }

    private RefreshToken(
        RefreshTokenId id,
        RefreshTokenFamilyId refreshTokenFamilyId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt
    )
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        FamilyId = refreshTokenFamilyId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(
        RefreshTokenId id,
        RefreshTokenFamilyId refreshTokenFamilyId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt
    )
    {
        return new RefreshToken(id, refreshTokenFamilyId, userId, tokenHash, expiresAt);
    }

    public void Revoke(RefreshTokenId? replacedByTokenId = null)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacedByTokenId;

        Touch();
    }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive => !IsExpired && !IsRevoked;

    public bool WasRotated => ReplacedByTokenId is not null;
}
