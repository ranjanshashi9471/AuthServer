using AuthServer.Domain.Common;

namespace AuthServer.Domain.ValueObjects.Identifiers;

public sealed record UserId : StronglyTypedId
{
    private UserId(Guid value)
        : base(value) { }

    public static UserId New() => new(Guid.CreateVersion7());

    public static UserId From(Guid value) => new(value);
}
