using AuthServer.Domain.Common;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class RolePermission : Entity<RolePermissionId>
{
    public RoleId RoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }

    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { }

    private RolePermission(RolePermissionId id, RoleId roleId, PermissionId permissionId)
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public static RolePermission Create(RoleId roleId, PermissionId permissionId)
    {
        return new RolePermission(RolePermissionId.New(), roleId, permissionId);
    }
}
