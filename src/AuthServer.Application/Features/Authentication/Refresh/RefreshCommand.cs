using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Refresh;

namespace AuthServer.Application.Features.Authentication.Refresh;

public sealed record RefreshCommand(
    string RefreshToken)
    : ICommand<RefreshResponse>;