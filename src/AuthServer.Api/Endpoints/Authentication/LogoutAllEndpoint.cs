using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.LogoutAll;
using AuthServer.Application.Messaging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class LogoutAllEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/auth/logout-all",
                async (ICommandBus commandBus, CancellationToken cancellationToken) =>
                {
                    await commandBus.Send(new LogoutAllCommand(), cancellationToken);

                    return Results.NoContent();
                }
            )
            .RequireAuthorization() // Mandatory: ensures ICurrentUser is populated
            .WithName("LogoutAll")
            .WithTags("Authentication")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
