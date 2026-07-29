namespace AuthServer.Application.Messaging.Abstractions;

public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    Task Handle(
        TCommand command,
        CancellationToken cancellationToken);
}