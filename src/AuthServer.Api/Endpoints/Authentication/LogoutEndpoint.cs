using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.Logout;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Logout;

public sealed class LogoutEnpoint : IEndpoint
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
            .WithTags("Authentication");
    }
}
