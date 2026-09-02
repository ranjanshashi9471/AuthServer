using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Features.Authentication.ResetPassword;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.ResetPassword;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/reset-password", HandleAsync)
            .WithName("ResetPassword")
            .WithTags("Authentication")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting(RateLimitingExtensions.ResetPassword)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var command = new ResetPasswordCommand(request.AccessToken, request.NewPassword);

        await commandBus.Send(command, cancellationToken);

        return Results.NoContent(); // 204 No Content instructs the client to redirect to /login
    }
}
