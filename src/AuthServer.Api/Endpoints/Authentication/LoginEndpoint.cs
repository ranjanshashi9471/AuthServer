using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authentication.Login;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/auth/login",
            HandleAsync)
        .WithName("Login")
        .WithTags("Authentication");
    }

    private static async Task<Ok<LoginResponse>> HandleAsync(
        LoginRequest request,
        ICommandBus commandBus,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var response = await commandBus.Send(
            command,
            cancellationToken);

        return TypedResults.Ok(response);
    }
}