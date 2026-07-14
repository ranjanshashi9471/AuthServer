namespace AuthServer.Contracts.Authentication;

public sealed record LoginResponse(
    string AccessToken);