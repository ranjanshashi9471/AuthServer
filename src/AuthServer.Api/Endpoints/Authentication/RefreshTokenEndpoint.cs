using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Features.Authentication.Refresh;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Refresh;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/auth/refresh", HandleAsync)
            .WithName("Refresh")
            .WithTags("Authentication")
            .RequireRateLimiting(RateLimitingExtensions.Refresh)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleAsync(
        RefreshRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var command = new RefreshCommand(request.RefreshToken);

        var response = await commandBus.Send(command, cancellationToken);

        return Results.Ok(response);
    }
}
