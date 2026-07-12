namespace AuthServer.Domain.Common;

public abstract class Entity<TId>
{
    protected Entity(
        TId id,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public TId Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    protected void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}