using AuthServer.Api.Abstractions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class RegisterUserEndpoint : IEndpoint
{
    #region Endpoint Registration

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", HandleAsync)
            .WithName("RegisterUser")
            .WithTags("Authentication")
            .RequireRateLimiting(RateLimitingExtensions.Register)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    #endregion

    #region Handlers

    private static async Task<Created<RegisterUserResponse>> HandleAsync(
        RegisterUserRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var response = await commandBus.Send(new RegisterUserCommand(request), cancellationToken);

        return TypedResults.Created(string.Empty, response);
    }

    #endregion
}
