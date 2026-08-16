using System.Security.Cryptography;
using System.Text;
using AuthServer.Application.Abstractions.Security;

namespace AuthServer.Infrastructure.Security;

internal sealed class SecretHasher : ISecretHasher
{
    public string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var bytes = Encoding.UTF8.GetBytes(secret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    public bool Verify(string secret, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        var computedHash = Hash(secret);

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(computedHash),
            Convert.FromHexString(hash)
        );
    }
}
