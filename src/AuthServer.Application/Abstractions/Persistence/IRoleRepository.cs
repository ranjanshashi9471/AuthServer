using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);
    Task<bool> IsNameDuplicateAsync(string name, CancellationToken cancellationToken = default);
    void Add(Role role);
}
