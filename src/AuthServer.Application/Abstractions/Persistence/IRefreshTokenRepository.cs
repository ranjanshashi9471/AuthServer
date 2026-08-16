using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByIdAsync(
        RefreshTokenId id,
        CancellationToken cancellationToken = default
    );

    Task Update(RefreshToken refreshToken);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );

    Task RevokeFamilyAsync(
        RefreshTokenFamilyId refreshTokenFamilyId,
        CancellationToken cancellationToken = default
    );

    Task RevokeAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
