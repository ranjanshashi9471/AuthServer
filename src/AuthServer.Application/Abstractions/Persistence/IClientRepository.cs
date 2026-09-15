using AuthServer.Domain.Entities;

namespace AuthServer.Application.Abstractions.Persistence;

public interface IClientRepository
{
    void Add(Client client);
}
