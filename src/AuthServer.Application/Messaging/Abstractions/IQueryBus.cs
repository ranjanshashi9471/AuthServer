namespace AuthServer.Application.Messaging.Abstractions;

public interface IQueryBus
{
    Task<TResult> Send<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default
    );
}
