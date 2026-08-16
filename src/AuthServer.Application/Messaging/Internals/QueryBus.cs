using AuthServer.Application.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Application.Messaging.Internals;

internal sealed class QueryBus : IQueryBus
{
    private readonly IServiceProvider _serviceProvider;

    public QueryBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> Send<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default
    )
    {
        var queryType = query.GetType();

        var handlerInterface = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));

        var handler = _serviceProvider.GetRequiredService(handlerInterface);

        var handleMethod =
            handlerInterface.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle))
            ?? throw new InvalidOperationException("Handle method not found.");

        var task =
            handleMethod.Invoke(handler, new object[] { query, cancellationToken }) as Task<TResult>
            ?? throw new InvalidOperationException("Invalid handler.");

        return await task;
    }
}
