using System.Security.Cryptography;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Infrastructure.Security.PasswordResetTokens;

internal sealed class PasswordResetTokenProvider : IPasswordResetTokenProvider
{
    private const char Separator = '.';

    public PasswordResetTokenData Generate()
    {
        Span<byte> bytes = stackalloc byte[32];

        RandomNumberGenerator.Fill(bytes);

        return new PasswordResetTokenData(PasswordResetTokenId.New(), Convert.ToHexString(bytes));
    }

    public string BuildToken(PasswordResetTokenData resetPasswordToken)
    {
        return $"{resetPasswordToken.Id.Value:N}{Separator}{resetPasswordToken.Secret}";
    }

    public bool TryParse(
        string token,
        out PasswordResetTokenId resetPasswordTokenId,
        out string secret
    )
    {
        resetPasswordTokenId = default!;

        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var separator = token.IndexOf(Separator);

        if (separator <= 0 || separator == token.Length - 1)
        {
            return false;
        }

        if (!Guid.TryParseExact(token.AsSpan(0, separator), "N", out var guid))
        {
            return false;
        }

        resetPasswordTokenId = PasswordResetTokenId.From(guid);

        secret = token[(separator + 1)..];

        return true;
    }
}
