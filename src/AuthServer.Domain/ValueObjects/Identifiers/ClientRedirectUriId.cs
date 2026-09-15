namespace AuthServer.Domain.ValueObjects.Identifiers;

public readonly record struct ClientRedirectUriId(Guid Value)
{
    public static ClientRedirectUriId New() => new(Guid.NewGuid());
}
