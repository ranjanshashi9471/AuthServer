using AuthServer.Application.Abstractions.Communication.Notifications;

namespace AuthServer.Infrastructure.Communication.Notifications;

internal sealed class EmailNotificationService : INotificationService
{
    public Task SendPasswordResetAsync(
        PasswordResetNotification notification,
        CancellationToken cancellationToken = default
    )
    {
        Console.WriteLine(notification.ResetToken);

        return Task.CompletedTask;
    }
}
