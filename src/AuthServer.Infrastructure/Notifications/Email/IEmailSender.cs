namespace AuthServer.Infrastructure.Notifications.Email;

internal interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
