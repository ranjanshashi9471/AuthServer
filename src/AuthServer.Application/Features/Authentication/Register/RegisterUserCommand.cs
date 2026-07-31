using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;

public sealed record RegisterUserCommand(RegisterUserRequest Request)
    : ICommand<RegisterUserResponse>;
