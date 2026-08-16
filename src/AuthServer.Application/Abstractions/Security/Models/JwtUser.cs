namespace AuthServer.Application.Abstractions.Security.Models;

public sealed record JwtUser(Guid UserId, string Email);
