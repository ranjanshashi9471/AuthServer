using AuthServer.Application.Abstractions.Notifications;

namespace AuthServer.Infrastructure.Notifications.Email;

internal sealed class EmailNotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;

    public EmailNotificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        return notification switch
        {
            EmailVerificationNotification n => SendVerificationEmailAsync(n, cancellationToken),
            PasswordResetNotification n => SendPasswordResetEmailAsync(n, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported notification type: {notification.GetType().Name}"
            ),
        };
    }

    private Task SendVerificationEmailAsync(
        EmailVerificationNotification notification,
        CancellationToken cancellationToken
    )
    {
        var message = new EmailMessage(
            notification.Email.Value,
            "Verify your email",
            BuildVerificationEmail(notification.Token)
        );

        return _emailSender.SendAsync(message, cancellationToken);
    }

    private Task SendPasswordResetEmailAsync(
        PasswordResetNotification notification,
        CancellationToken cancellationToken
    )
    {
        var message = new EmailMessage(
            notification.Destination.Value,
            "Reset your password",
            BuildPasswordResetEmail(notification.ResetToken)
        );

        return _emailSender.SendAsync(message, cancellationToken);
    }

    private static string BuildVerificationEmail(string token)
    {
        return $"""
            <html>
                <body>
                    <h2>Verify your email</h2>
                    <p>Use the following token to verify your account:</p>
                    <p>{token}</p>
                </body>
            </html>
            """;
    }

    private static string BuildPasswordResetEmail(string token)
    {
        return $"""
            <html>
                <body>
                    <h2>Password reset</h2>
                    <p>Use the following token to reset your password:</p>
                    <p>{token}</p>
                </body>
            </html>
            """;
    }
}
