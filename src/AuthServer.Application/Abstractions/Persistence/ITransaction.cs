namespace AuthServer.Application.Abstractions.Persistence;

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
