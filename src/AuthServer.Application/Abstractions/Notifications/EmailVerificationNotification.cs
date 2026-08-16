using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Abstractions.Notifications;

public sealed record EmailVerificationNotification(Email Email, string Token) : Notification;
