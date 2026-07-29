using System.Security.Cryptography;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Infrastructure.Security.RefreshTokens;

internal sealed class RefreshTokenProvider
    : IRefreshTokenProvider
{
    private const char Separator = '.';

    public RefreshTokenData Generate()
    {
        Span<byte> bytes = stackalloc byte[32];

        RandomNumberGenerator.Fill(bytes);

        return new RefreshTokenData(
            RefreshTokenId.New(),
            Convert.ToHexString(bytes));
    }

    public string BuildToken(
        RefreshTokenData refreshToken)
    {
        return $"{refreshToken.Id.Value:N}{Separator}{refreshToken.Secret}";
    }

    public bool TryParse(
        string token,
        out RefreshTokenId refreshTokenId,
        out string secret)
    {
        refreshTokenId = default!;
        secret = string.Empty;

        var separator = token.IndexOf(Separator);

        if (separator <= 0 ||
            separator == token.Length - 1)
        {
            return false;
        }

        if (!Guid.TryParseExact(
            token.AsSpan(0, separator),
            "N",
            out var guid))
        {
            return false;
        }

        refreshTokenId = RefreshTokenId.From(guid);
        secret = token[(separator + 1)..];

        return true;
    }
}