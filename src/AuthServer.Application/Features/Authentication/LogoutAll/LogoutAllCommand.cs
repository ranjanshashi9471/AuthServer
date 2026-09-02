using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.LogoutAll;

public sealed record LogoutAllCommand() : ICommand;
