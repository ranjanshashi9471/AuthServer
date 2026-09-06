using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Security;

internal sealed class PermissionService : IPermissionService
{
    private readonly AuthDbContext _context;

    public PermissionService(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<HashSet<string>> GetPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await _context
            .Set<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Join(
                _context.Set<RolePermission>(),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp.PermissionId
            )
            .Join(
                _context.Permissions,
                permissionId => permissionId,
                p => p.Id,
                (permissionId, p) => p.Name
            )
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. permissions];
    }
}
