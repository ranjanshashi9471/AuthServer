using AuthServer.Contracts.Authentication;
using AuthServer.Application.Messaging.Abstractions;

public sealed record RegisterUserCommand(
    RegisterUserRequest Request)
    : ICommand<RegisterUserResponse>;