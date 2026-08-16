namespace AuthServer.Infrastructure.Notifications.Email;

internal sealed record EmailMessage(string To, string Subject, string HtmlBody);
