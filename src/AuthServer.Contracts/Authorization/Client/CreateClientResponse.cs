namespace AuthServer.Contracts.Authorization.Client;

public sealed record CreateClientResponse(string ClientId, string Name, string Type);
