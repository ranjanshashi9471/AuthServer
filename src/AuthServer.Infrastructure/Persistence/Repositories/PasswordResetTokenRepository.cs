using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AuthDbContext _context;

    public PasswordResetTokenRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default
    )
    {
        // Synchronous Add is faster because it only modifies the in-memory Change Tracker
        _context.PasswordResetTokens.Add(resetToken);

        return Task.CompletedTask;
    }

    public Task<PasswordResetToken?> GetByIdAsync(
        PasswordResetTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        return _context
            .PasswordResetTokens.Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    )
    {
        // Extracting 'now' enables EF Core query plan caching
        var now = DateTimeOffset.UtcNow;

        // Retained async/await because Task<List<T>> does not implicitly cast to Task<IReadOnlyList<T>>.
        return await _context
            .PasswordResetTokens.Where(prt =>
                prt.UserId == userId && prt.UsedAt == null && prt.ExpiresAt > now
            )
            .ToListAsync(cancellationToken);
    }

    public void Update(PasswordResetToken resetToken)
    {
        _context.PasswordResetTokens.Update(resetToken);
    }

    public async Task<bool> MarkAsUsedAsync(
        PasswordResetTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;

        var rowsAffected = await _context
            .PasswordResetTokens.Where(prt =>
                prt.Id == id && prt.UsedAt == null && prt.ExpiresAt > now
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(prt => prt.UsedAt, now)
                        .SetProperty(prt => prt.UpdatedAt, now),
                cancellationToken
            );

        return rowsAffected > 0;
    }
}
