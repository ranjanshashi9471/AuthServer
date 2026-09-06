using AuthServer.Api.Abstractions;
using AuthServer.Api.Authorization;
using AuthServer.Application.Abstractions.Security;
using AppPermissions = AuthServer.Application.Abstractions.Security.Permissions;

namespace AuthServer.Api.Endpoints.Authorization;

public sealed class AuthorizationTestEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/authorization-test", () => Results.Ok(new { message = "You have users.read" }))
            .RequirePermission(AppPermissions.UsersRead)
            .WithName("AuthorizationTest")
            .WithTags("Test");
    }
}
