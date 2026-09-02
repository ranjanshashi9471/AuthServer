using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Features.Authentication.ResendVerification;
using AuthServer.Application.Features.Authentication.VerifyEmail;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.ResendVerification;
using AuthServer.Contracts.Authentication.VerifyEmail;

namespace AuthServer.Api.Endpoints.Authentication;

public class EmailVerificationEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group
            .MapPost(
                "/verify-email",
                async (
                    VerifyEmailRequest request,
                    ICommandBus commandBus,
                    CancellationToken cancellationToken
                ) =>
                {
                    await commandBus.Send(new VerifyEmailCommand(request.Token), cancellationToken);
                    return Results.NoContent();
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.VerifyEmail)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        ;

        group
            .MapPost(
                "/resend-verification",
                async (
                    ResendVerificationRequest request,
                    ICommandBus commandBus,
                    CancellationToken cancellationToken
                ) =>
                {
                    await commandBus.Send(
                        new ResendVerificationCommand(request.Email),
                        cancellationToken
                    );
                    return Results.NoContent();
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.VerifyEmail)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
