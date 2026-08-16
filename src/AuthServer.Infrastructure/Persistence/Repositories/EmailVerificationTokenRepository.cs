using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AuthDbContext _context;

    public EmailVerificationTokenRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default
    )
    {
        // Synchronous Add is faster because it only modifies the in-memory Change Tracker
        _context.EmailVerificationTokens.Add(token);

        return Task.CompletedTask;
    }

    public Task<EmailVerificationToken?> GetByIdAsync(
        EmailVerificationTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        // Elided async/await: passing the Task directly
        return _context
            .EmailVerificationTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task Update(EmailVerificationToken token)
    {
        // Purely synchronous in-memory operation. DB is hit during UnitOfWork.SaveChangesAsync()
        _context.EmailVerificationTokens.Update(token);

        return Task.CompletedTask;
    }

    public Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;

        // Elided async/await: passing the Task directly
        return _context
            .EmailVerificationTokens.Where(t =>
                t.UserId == userId
                && t.UsedAt == null
                && t.InvalidatedAt == null
                && t.ExpiresAt > now
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(t => t.InvalidatedAt, now)
                        .SetProperty(t => t.UpdatedAt, now),
                cancellationToken
            );
    }
}
