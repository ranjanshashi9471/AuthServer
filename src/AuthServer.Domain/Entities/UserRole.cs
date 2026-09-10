using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class UserRole
{
    public UserId UserId { get; private set; } = null!;

    public RoleId RoleId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;

    public Role Role { get; private set; } = null!;

    private UserRole() { }

    private UserRole(UserId userId, RoleId roleId)
    {
        UserId = userId;
        RoleId = roleId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static UserRole Create(UserId userId, RoleId roleId)
    {
        return new UserRole(userId, roleId);
    }
}
