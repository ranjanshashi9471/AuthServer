using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface IRefreshTokenProvider
{
    RefreshTokenData Generate();

    string BuildToken(RefreshTokenData refreshToken);

    bool TryParse(
        string token,
        out RefreshTokenId refreshTokenId,
        out string secret);
}