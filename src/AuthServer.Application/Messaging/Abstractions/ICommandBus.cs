namespace AuthServer.Application.Messaging.Abstractions;

public interface ICommandBus
{
    Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);
}