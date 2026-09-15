using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;

namespace AuthServer.Infrastructure.Persistence.Repositories;

internal sealed class ClientRepository : IClientRepository
{
    private readonly AuthDbContext _dbContext;

    public ClientRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Client client)
    {
        _dbContext.Set<Client>().Add(client);
    }
}
