using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        // Synchronous Add is faster because it only modifies the in-memory Change Tracker
        _context.RefreshTokens.Add(refreshToken);

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByIdAsync(
        RefreshTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        // Elided async/await: passing the Task directly.
        // FirstOrDefaultAsync executes a 'LIMIT 1' query, optimal for Primary Key lookups.
        return _context
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;

        // Retained async/await because Task<List<T>> does not implicitly cast to Task<IReadOnlyList<T>>.
        // The await unwraps the list, allowing the implicit cast, and the method signature wraps it back.
        return await _context
            .RefreshTokens.Where(rt =>
                rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now
            )
            .ToListAsync(cancellationToken);
    }

    public Task Update(RefreshToken refreshToken)
    {
        // Purely synchronous in-memory operation. DB is hit during UnitOfWork.SaveChangesAsync()
        _context.RefreshTokens.Update(refreshToken);

        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(
        RefreshTokenFamilyId refreshTokenFamilyId,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;

        // Elided async/await: passing the Task directly.
        return _context
            .RefreshTokens.Where(t => t.FamilyId == refreshTokenFamilyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(rt => rt.RevokedAt, now)
                        .SetProperty(rt => rt.UpdatedAt, now),
                cancellationToken
            );
    }

    public Task RevokeAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Elided async/await: passing the Task directly.
        return _context
            .RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.UpdatedAt, now),
                cancellationToken
            );
    }
}
