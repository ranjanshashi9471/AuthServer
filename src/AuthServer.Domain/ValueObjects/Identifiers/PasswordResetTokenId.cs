using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record PasswordResetTokenId : StronglyTypedId
{
    private PasswordResetTokenId(Guid value)
        : base(value) { }

    public static PasswordResetTokenId New() => new(Guid.CreateVersion7());

    public static PasswordResetTokenId From(Guid value) => new(value);
}
