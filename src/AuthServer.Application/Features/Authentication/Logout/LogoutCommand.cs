using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(
    string RefreshToken)
    : ICommand; 