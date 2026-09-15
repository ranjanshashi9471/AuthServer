using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authorization.Client;
using AuthServer.Domain.Enums;

namespace AuthServer.Application.Features.Authorization.Clients.CreateClient;

public sealed record CreateClientCommand(string Name, ClientType Type, List<string> RedirectUris)
    : ICommand<CreateClientResponse>;
