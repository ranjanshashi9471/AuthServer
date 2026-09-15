namespace AuthServer.Contracts.Authorization.Client;

public sealed record CreateClientRequest(string Name, string Type, List<string> RedirectUris);
