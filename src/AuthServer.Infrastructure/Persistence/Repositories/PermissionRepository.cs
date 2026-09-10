using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly AuthDbContext _context;

    public PermissionRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<Permission?> GetByIdAsync(
        PermissionId id,
        CancellationToken cancellationToken = default
    )
    {
        return _context.Permissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<bool> IsNameDuplicateAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        return _context.Permissions.AnyAsync(p => p.Name == name, cancellationToken);
    }

    public Task<HashSet<PermissionId>> GetExistingIdsAsync(
        IEnumerable<PermissionId> ids,
        CancellationToken cancellationToken = default
    )
    {
        var idList = ids.ToList();

        return _context
            .Permissions.Where(p => idList.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSetAsync(cancellationToken);
    }

    public void Add(Permission permission)
    {
        _context.Permissions.Add(permission);
    }

    public async Task<HashSet<PermissionId>> GetPermissionIdsForRoleAsync(
        RoleId roleId,
        CancellationToken cancellationToken = default
    )
    {
        var permissionIds = await _context
            .Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        return [.. permissionIds];
    }
}
