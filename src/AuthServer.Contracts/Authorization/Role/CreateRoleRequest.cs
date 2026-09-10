namespace AuthServer.Contracts.Authorization.Role;

public sealed record CreateRoleRequest(
    string Name,
    string Description,
    IReadOnlyCollection<Guid> PermissionIds
);
