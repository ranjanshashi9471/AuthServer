using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default);

    Task<bool> IsNameDuplicateAsync(string name, CancellationToken cancellationToken = default);

    Task<HashSet<PermissionId>> GetExistingIdsAsync(
        IEnumerable<PermissionId> ids,
        CancellationToken cancellationToken = default
    );

    void Add(Permission permission);
}
