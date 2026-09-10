using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Features.Authorization.Roles.AssignRole;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Api.Endpoints.Users;

public sealed class AssignRole : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/users/{userId:guid}/roles/{roleId:guid}",
                async (
                    Guid userId,
                    Guid roleId,
                    ICommandBus sender,
                    CancellationToken cancellationToken
                ) =>
                {
                    var command = new AssignRoleCommand(UserId.From(userId), RoleId.From(roleId));

                    await sender.Send(command, cancellationToken);

                    return Results.NoContent();
                }
            )
            .WithTags("Users")
            .RequirePermission(Permissions.UsersAssignRole);
    }
}
