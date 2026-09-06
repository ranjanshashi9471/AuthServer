using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Roles.AssignRole;

public sealed record AssignRoleCommand(UserId UserId, RoleId RoleId) : ICommand;
