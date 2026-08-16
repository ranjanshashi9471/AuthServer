using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record EmailVerificationTokenId : StronglyTypedId
{
    public EmailVerificationTokenId(Guid value)
        : base(value) { }

    public static EmailVerificationTokenId New() => new(Guid.CreateVersion7());

    public static EmailVerificationTokenId From(Guid value) => new(value);
}
