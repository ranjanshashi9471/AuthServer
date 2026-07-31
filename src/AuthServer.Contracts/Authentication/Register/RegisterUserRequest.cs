namespace AuthServer.Contracts.Authentication;

public sealed record RegisterUserRequest(
    string Email,
    string Username,
    string Password);