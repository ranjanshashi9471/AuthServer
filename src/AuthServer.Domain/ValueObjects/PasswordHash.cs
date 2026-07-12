using AuthServer.Domain.Exceptions;

namespace AuthServer.Domain.ValueObjects;

public sealed record PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash From(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ValidationException("Password hash cannot be empty.");

        return new PasswordHash(hash);
    }

    public override string ToString() => Value;
}