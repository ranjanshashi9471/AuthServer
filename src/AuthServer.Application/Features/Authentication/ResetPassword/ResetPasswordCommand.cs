using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.ResetPassword;

public sealed record ResetPasswordCommand(string AccessToken, string NewPassword) : ICommand;
