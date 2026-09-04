using AuthServer.Domain.Common;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class Role : Entity<RoleId>
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<RolePermission> _rolePermissions = [];

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    private Role() { }

    private Role(RoleId id, string name, string description)
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        Name = name;
        Description = description;
    }

    public static Role Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleViolationException("Role name cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleViolationException("Role description cannot be empty.");

        return new Role(RoleId.New(), name.Trim(), description.Trim());
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleViolationException("Role description cannot be empty.");

        Description = description.Trim();
        Touch();
    }
}
