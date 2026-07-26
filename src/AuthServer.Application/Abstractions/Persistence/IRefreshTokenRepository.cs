using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByIdAsync(
    RefreshTokenId id,
    CancellationToken cancellationToken = default);

    void Update(RefreshToken refreshToken);
}