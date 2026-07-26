namespace AuthServer.Domain.Common;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(
        TId id,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }


    protected void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}