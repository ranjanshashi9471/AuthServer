using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.User.CurrentUser;

namespace AuthServer.Application.Features.Users.CurrentUser;

public sealed record CurrentUserQuery : IQuery<CurrentUserResponse>;
