using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        _context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default
    ) => _context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default
    ) => _context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }
}
