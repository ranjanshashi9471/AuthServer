using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default
    );
}
