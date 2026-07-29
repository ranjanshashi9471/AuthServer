using AuthServer.Api.Abstractions;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AuthServer.Api.Endpoints.Authentication;

public sealed class RegisterUserEndpoint : IEndpoint
{
    #region Endpoint Registration

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/auth/register",
            HandleAsync)
        .WithName("RegisterUser")
        .WithTags("Authentication");
    }

    #endregion

    #region Handlers

    private static async Task<Created<RegisterUserResponse>> HandleAsync(
        RegisterUserRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken)
    {
        var response = await commandBus.Send(
            new RegisterUserCommand(request),
            cancellationToken);

        return TypedResults.Created(string.Empty, response);
    }

    #endregion
}