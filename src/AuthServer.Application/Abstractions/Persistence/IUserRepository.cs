using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
