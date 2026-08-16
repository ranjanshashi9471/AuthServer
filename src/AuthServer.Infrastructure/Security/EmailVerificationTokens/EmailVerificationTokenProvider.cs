using System.Security.Cryptography;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Infrastructure.Security.EmailVerificationTokens;

internal sealed class EmailVerificationTokenProvider : IEmailVerificationTokenProvider
{
    private const char Separator = '.';
    private const int SecretByteLength = 32;
    private const int SecretHexLength = SecretByteLength * 2; // 64 chars

    public EmailVerificationTokenData Generate()
    {
        Span<byte> bytes = stackalloc byte[SecretByteLength];
        RandomNumberGenerator.Fill(bytes);

        return new EmailVerificationTokenData(
            EmailVerificationTokenId.New(),
            Convert.ToHexString(bytes)
        );
    }

    public string BuildToken(EmailVerificationTokenData tokenData)
    {
        return $"{tokenData.Id.Value:N}{Separator}{tokenData.Secret}";
    }

    public bool TryParse(string token, out EmailVerificationTokenId tokenId, out string secret)
    {
        tokenId = default!;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var firstDotIndex = token.IndexOf(Separator);
        var lastDotIndex = token.LastIndexOf(Separator);

        // Must contain exactly one dot separator
        if (
            firstDotIndex <= 0
            || firstDotIndex != lastDotIndex
            || firstDotIndex == token.Length - 1
        )
        {
            return false;
        }

        var idSpan = token.AsSpan(0, firstDotIndex);
        var secretSpan = token.AsSpan(firstDotIndex + 1);

        // Enforce exact 64-character hex secret length
        if (secretSpan.Length != SecretHexLength)
        {
            return false;
        }

        if (!Guid.TryParseExact(idSpan, "N", out var guid))
        {
            return false;
        }

        tokenId = EmailVerificationTokenId.From(guid);
        secret = secretSpan.ToString();

        return true;
    }
}
