using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Features.Authorization.Permissions.CreatePermission;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authorization.Permission;
using AppPermissions = AuthServer.Application.Abstractions.Security.Permissions;

namespace AuthServer.Api.Endpoints.Authorization.Permissions;

public sealed class CreatePermission : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/permissions",
                async (
                    CreatePermissionRequest request,
                    ICommandBus sender,
                    CancellationToken cancellationToken
                ) =>
                {
                    var command = new CreatePermissionCommand(request.Name, request.Description);

                    var permissionId = await sender.Send(command, cancellationToken);

                    return Results.Created(
                        $"/api/permissions/{permissionId.Value}",
                        new { Id = permissionId.Value }
                    );
                }
            )
            .WithTags("Permissions")
            .RequirePermission(AppPermissions.PermissionsCreate);
    }
}
