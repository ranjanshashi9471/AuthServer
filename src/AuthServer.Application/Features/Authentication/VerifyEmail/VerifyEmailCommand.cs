using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : ICommand;
