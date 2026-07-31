using AuthServer.Domain.Exceptions;

namespace AuthServer.Domain.ValueObjects;

public sealed record Username
{
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("Username is required.");

        value = value.Trim();

        if (value.Length < 3)
            throw new ValidationException("Username must be at least 3 characters.");

        if (value.Length > 50)
            throw new ValidationException("Username cannot exceed 50 characters.");

        return new Username(value);
    }

    public override string ToString() => Value;
}
