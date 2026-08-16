using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security.Models;

public sealed record EmailVerificationTokenData(EmailVerificationTokenId Id, string Secret);
