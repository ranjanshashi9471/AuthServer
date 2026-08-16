using AuthServer.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace AuthServer.Infrastructure.Notifications.Development;

internal sealed class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        switch (notification)
        {
            case EmailVerificationNotification emailVerification:
                _logger.LogInformation(
                    "[DEV EMAIL VERIFICATION] To: {Email} | Token: {Token}",
                    emailVerification.Email.Value,
                    emailVerification.Token
                );
                break;

            case PasswordResetNotification passwordReset:
                _logger.LogInformation(
                    "[DEV PASSWORD RESET] To: {Email} | Token: {Token}",
                    passwordReset.Destination.Value,
                    passwordReset.ResetToken
                );
                break;

            default:
                _logger.LogWarning(
                    "[DEV NOTIFICATION] Unsupported notification type: {Type}",
                    notification.GetType().Name
                );
                break;
        }

        return Task.CompletedTask;
    }
}
