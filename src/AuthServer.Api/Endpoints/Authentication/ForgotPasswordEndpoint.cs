using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.ForgotPassword;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.ForgotPassword;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/forgot-password", HandleAsync)
            .WithName("ForgotPassword")
            .WithTags("Authentication")
            .RequireRateLimiting(Extensions.RateLimitingExtensions.ForgotPassword)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var command = new ForgotPasswordCommand(request.Email);

        await commandBus.Send(command, cancellationToken);

        return Results.NoContent();
    }
}
