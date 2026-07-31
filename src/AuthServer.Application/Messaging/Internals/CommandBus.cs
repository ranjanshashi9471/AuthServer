using AuthServer.Application.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Application.Messaging.Internals;

public sealed class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;

    public CommandBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();

        var handlerInterface = typeof(ICommandHandler<>).MakeGenericType(commandType);

        var handler = _serviceProvider.GetRequiredService(handlerInterface);

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command '{commandType.Name}'."
            );
        }

        var handleMethod =
            handlerInterface.GetMethod(nameof(ICommandHandler<ICommand>.Handle))
            ?? throw new InvalidOperationException(
                $"Handle method not found on '{handlerInterface.Name}'."
            );

        var task =
            handleMethod.Invoke(handler, new[] { (object)command, cancellationToken }) as Task
            ?? throw new InvalidOperationException("Handler did not return the expected task.");

        await task;
    }

    public async Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default
    )
    {
        // Get the runtime type of the command.
        var commandType = command.GetType();

        // Build ICommandHandler<TCommand, TResult>.
        var handlerInterface = typeof(ICommandHandler<,>).MakeGenericType(
            commandType,
            typeof(TResult)
        );

        // Resolve the handler from DI.
        var handler = _serviceProvider.GetRequiredService(handlerInterface);

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command '{commandType.Name}'."
            );
        }

        // Find the Handle method.
        var handleMethod =
            handlerInterface.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle))
            ?? throw new InvalidOperationException(
                $"Handle method not found on '{handlerInterface.Name}'."
            );

        // Invoke Handle(...)
        var task =
            handleMethod.Invoke(handler, new[] { (object)command, cancellationToken })
                as Task<TResult>
            ?? throw new InvalidOperationException("Handler did not return the expected task.");
        return await task;
    }
}
