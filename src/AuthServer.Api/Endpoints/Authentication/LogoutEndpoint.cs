using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.Logout;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Logout;
using Microsoft.AspNetCore.Http; // Needed for StatusCodes

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class LogoutEndpoint : IEndpoint // <-- Fixed typo here
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/auth/logout",
                async (
                    LogoutRequest request,
                    ICommandBus commandBus,
                    CancellationToken cancellationToken
                ) =>
                {
                    await commandBus.Send(
                        new LogoutCommand(request.RefreshToken),
                        cancellationToken
                    );

                    return Results.NoContent();
                }
            )
            .WithName("Logout")
            .WithTags("Authentication")
            .Produces(StatusCodes.Status204NoContent);
    }
}
