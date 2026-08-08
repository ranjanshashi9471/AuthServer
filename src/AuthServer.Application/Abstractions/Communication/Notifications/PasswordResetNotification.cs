using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Abstractions.Communication.Notifications;

public sealed record PasswordResetNotification(
    Email Destination,
    string ResetToken,
    string ResetUrl
);
