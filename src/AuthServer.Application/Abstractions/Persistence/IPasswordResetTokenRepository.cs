using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetByIdAsync(
        PasswordResetTokenId id,
        CancellationToken cancellationToken = default
    );

    void Update(PasswordResetToken refreshToken);

    Task<IReadOnlyList<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> MarkAsUsedAsync(
        PasswordResetTokenId id,
        CancellationToken cancellationToken = default
    );
}
