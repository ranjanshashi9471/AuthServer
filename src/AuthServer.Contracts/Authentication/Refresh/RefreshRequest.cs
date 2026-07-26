namespace AuthServer.Contracts.Authentication.Refresh;

public sealed record RefreshRequest(
    string RefreshToken);