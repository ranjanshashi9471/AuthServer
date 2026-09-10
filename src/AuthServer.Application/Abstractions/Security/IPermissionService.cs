using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );

    // New method for strict possession boundary
    Task<HashSet<PermissionId>> GetPermissionIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );

    Task<HashSet<PermissionId>> GetPermissionIdsForRoleAsync(
        RoleId roleId,
        CancellationToken cancellationToken = default
    );
}
