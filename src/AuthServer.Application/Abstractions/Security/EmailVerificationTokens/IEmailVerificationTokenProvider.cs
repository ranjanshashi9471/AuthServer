using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface IEmailVerificationTokenProvider
{
    EmailVerificationTokenData Generate();
    string BuildToken(EmailVerificationTokenData tokenData);
    bool TryParse(string token, out EmailVerificationTokenId tokenId, out string secret);
}
