namespace AuthServer.Application.Abstractions.Communication.Notifications;

public interface INotificationService
{
    Task SendPasswordResetAsync(
        PasswordResetNotification notification,
        CancellationToken cancellationToken = default
    );
}
