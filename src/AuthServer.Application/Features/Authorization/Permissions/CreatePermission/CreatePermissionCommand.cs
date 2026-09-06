using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Permissions.CreatePermission;

public sealed record CreatePermissionCommand(string Name, string Description)
    : ICommand<PermissionId>;
