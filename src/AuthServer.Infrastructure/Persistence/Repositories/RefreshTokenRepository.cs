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

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByIdAsync(
        RefreshTokenId id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .RefreshTokens.Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }
}
