using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
    Task<EmailVerificationToken?> GetByIdAsync(
        EmailVerificationTokenId id,
        CancellationToken cancellationToken = default
    );
    Task Update(EmailVerificationToken token);

    public Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );

    Task<DateTimeOffset?> GetLatestCreatedAtByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );
}
