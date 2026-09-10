namespace AuthServer.Domain.ValueObjects.Identifiers;

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New() => new(Guid.NewGuid());

    public static PermissionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Permission ID cannot be empty.", nameof(value));

        return new PermissionId(value);
    }
}
