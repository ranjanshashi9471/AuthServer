using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;

namespace AuthServer.Application.Features.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
