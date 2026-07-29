namespace AuthServer.Contracts.Authentication.Logout;

public sealed record LogoutRequest(
    string RefreshToken);