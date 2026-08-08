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

    public async Task AddAsync(
        PasswordResetToken resetToken,
        CancellationToken cancellationToken = default
    )
    {
        await _context.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByIdAsync(
        PasswordResetTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .PasswordResetTokens.Include(prt => prt.User)
            .SingleOrDefaultAsync(prt => prt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .PasswordResetTokens.Where(prt =>
                prt.UserId == userId && prt.UsedAt == null && prt.ExpiresAt > DateTimeOffset.UtcNow
            )
            .ToListAsync(cancellationToken);
    }

    public void Update(PasswordResetToken resetToken)
    {
        _context.PasswordResetTokens.Update(resetToken);
    }
}
