using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.ResendVerification;

public record ResendVerificationCommand(string Email) : ICommand;
