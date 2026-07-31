using System.Text.RegularExpressions;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Domain.ValueObjects;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("Email cannot be empty.");

        value = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(value))
            throw new ValidationException("Invalid email address.");

        return new Email(value);
    }

    public override string ToString() => Value;
}
