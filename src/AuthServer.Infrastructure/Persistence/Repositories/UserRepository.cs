using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // FirstOrDefaultAsync compiles to LIMIT 1, making it faster for PK lookups
        return _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        // Optimal for Unique columns
        return _context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        // AnyAsync is perfectly optimal (Translates to IF EXISTS(...) in SQL)
        return _context.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // Synchronous Add is recommended by EF Core for in-memory change tracking
        _context.Users.Add(user);

        return Task.CompletedTask;
    }
}
