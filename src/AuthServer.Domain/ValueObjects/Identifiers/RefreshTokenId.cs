using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record RefreshTokenId : StronglyTypedId
{
    private RefreshTokenId(Guid value)
        : base(value) { }

    public static RefreshTokenId New() => new(Guid.CreateVersion7());

    public static RefreshTokenId From(Guid value) => new(value);
}
