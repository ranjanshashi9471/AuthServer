namespace AuthServer.Application.Abstractions.Security;

public sealed record JwtUser(
    Guid UserId,
    string Email);