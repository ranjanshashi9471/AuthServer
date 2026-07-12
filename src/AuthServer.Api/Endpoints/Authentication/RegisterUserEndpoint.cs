using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.Register;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;

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

    private static async Task<IResult> HandleAsync(
        RegisterUserRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken)
    {
        var response = await commandBus.Send(
            new RegisterUserCommand(request),
            cancellationToken);

        return Results.Created(
            $"/users/{response.UserId}",
            response);
    }

    #endregion
}