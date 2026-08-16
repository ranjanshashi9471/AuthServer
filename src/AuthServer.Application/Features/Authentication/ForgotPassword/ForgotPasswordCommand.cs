using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
