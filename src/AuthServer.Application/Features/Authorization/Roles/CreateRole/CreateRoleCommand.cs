using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Roles.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    string Description,
    HashSet<PermissionId> PermissionIds
) : ICommand<RoleId>;
