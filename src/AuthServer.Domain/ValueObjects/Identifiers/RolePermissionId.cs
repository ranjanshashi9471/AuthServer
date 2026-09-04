using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record RolePermissionId : StronglyTypedId
{
    private RolePermissionId(Guid value)
        : base(value) { }

    public static RolePermissionId New() => new(Guid.NewGuid());

    public static RolePermissionId From(Guid value) => new(value);
}
