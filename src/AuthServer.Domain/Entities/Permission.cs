using AuthServer.Domain.Common;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class Permission : Entity<PermissionId>
{
    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    private Permission() { }

    private Permission(PermissionId id, string name, string description)
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        Name = name;
        Description = description;
    }

    public static Permission Create(string name, string description)
    {
        // Canonical representation
        var normalizedName = name.Trim().ToLowerInvariant();
        var normalizedDescription = description.Trim();

        return new Permission(
            PermissionId.From(Guid.NewGuid()),
            normalizedName,
            normalizedDescription
        );
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleViolationException("Permission description cannot be empty.");

        Description = description.Trim();
        Touch();
    }
}
