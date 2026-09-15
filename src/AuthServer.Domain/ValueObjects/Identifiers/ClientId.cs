namespace AuthServer.Domain.ValueObjects.Identifiers;

public readonly record struct ClientId(Guid Value)
{
    public static ClientId New() => new(Guid.NewGuid());
}
