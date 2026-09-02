using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Users.CurrentUser;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.User.CurrentUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Endpoints.Users;

public sealed class CurrentUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/users/me",
                async (IQueryBus queryBus, CancellationToken cancellationToken) =>
                {
                    var response = await queryBus.Send(new CurrentUserQuery(), cancellationToken);

                    return Results.Ok(response);
                }
            )
            .RequireAuthorization() // Ensures the request is authenticated before hitting the handler
            .WithName("GetCurrentUser")
            .WithTags("Users")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
