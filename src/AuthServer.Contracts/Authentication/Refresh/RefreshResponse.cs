namespace AuthServer.Contracts.Authentication.Refresh;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);
