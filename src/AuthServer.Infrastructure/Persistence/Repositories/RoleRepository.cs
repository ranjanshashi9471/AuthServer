using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        return _context.Roles.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

    public Task<bool> IsNameDuplicateAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        // Enforce case-insensitive comparison at the EF level
        var normalizedName = name.Trim().ToLower();

        return _context.Roles.AnyAsync(
            role => role.Name.ToLower() == normalizedName,
            cancellationToken
        );
    }

    public void Add(Role role)
    {
        _context.Roles.Add(role);
    }
}
