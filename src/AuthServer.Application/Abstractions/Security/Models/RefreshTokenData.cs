using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security.Models;

public sealed record RefreshTokenData(
    RefreshTokenId Id,
    string Secret);