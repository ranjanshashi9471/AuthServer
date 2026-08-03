using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Users.ChangePassword;

public sealed record ChangePasswordCommand(string OldPassword, string NewPassword) : ICommand;
