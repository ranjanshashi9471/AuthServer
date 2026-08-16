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
        // Elided async/await: passing the Task directly.
        // FirstOrDefaultAsync executes a 'LIMIT 1' query, optimal for Primary Key lookups.
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
}
