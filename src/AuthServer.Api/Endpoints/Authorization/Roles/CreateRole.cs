using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Features.Authorization.Roles.CreateRole;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Api.Endpoints.Roles;

public sealed record CreateRoleRequest(
    string Name,
    string Description,
    HashSet<Guid> PermissionIds
);

public sealed class CreateRole : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/roles",
                async (
                    CreateRoleRequest request,
                    ICommandBus sender,
                    CancellationToken cancellationToken
                ) =>
                {
                    var command = new CreateRoleCommand(
                        request.Name,
                        request.Description,
                        request.PermissionIds.Select(PermissionId.From).ToHashSet()
                    );

                    var roleId = await sender.Send(command, cancellationToken);

                    return Results.Created($"/api/roles/{roleId.Value}", new { Id = roleId.Value });
                }
            )
            .WithTags("Roles")
            .RequirePermission(Permissions.RolesCreate); // Your custom authorization extension
    }
}
