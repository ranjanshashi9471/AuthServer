using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Users.CurrentUser;
using AuthServer.Application.Messaging.Abstractions;

namespace AuthServer.Api.Endpoints.Users;

public sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/users/me", HandleAsync)
            .WithName("GetCurrentUser")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        IQueryBus queryBus,
        CancellationToken cancellationToken
    )
    {
        var response = await queryBus.Send(new CurrentUserQuery(), cancellationToken);

        return Results.Ok(response);
    }
}
