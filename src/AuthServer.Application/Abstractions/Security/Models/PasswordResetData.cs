using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security.Models;

public sealed record PasswordResetTokenData(PasswordResetTokenId Id, string Secret);
