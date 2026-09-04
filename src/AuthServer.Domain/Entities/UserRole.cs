using AuthServer.Domain.Common;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class UserRole : Entity<Guid>
{
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() { }

    private UserRole(UserId userId, RoleId roleId)
        : base(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public static UserRole Create(UserId userId, RoleId roleId)
    {
        return new UserRole(userId, roleId);
    }
}
