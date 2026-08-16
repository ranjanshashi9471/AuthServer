namespace AuthServer.Application.Messaging.Abstractions;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
