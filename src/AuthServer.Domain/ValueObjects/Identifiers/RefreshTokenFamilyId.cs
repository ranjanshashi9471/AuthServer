using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record RefreshTokenFamilyId : StronglyTypedId
{
    public RefreshTokenFamilyId(Guid value)
        : base(value) { }

    public static RefreshTokenFamilyId New() => new(Guid.CreateVersion7());

    public static RefreshTokenFamilyId From(Guid value) => new(value);
}
