namespace AuthServer.Application.Abstractions.Notifications;

public interface INotificationService
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
