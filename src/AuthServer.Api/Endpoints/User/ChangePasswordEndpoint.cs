using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Users.ChangePassword;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.User.ChangePassword;

namespace AuthServer.Api.Endpoints.Users;

public sealed class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/change-password", HandleAsync)
            .WithName("ChangePassword")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest changePasswordRequest,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var command = new ChangePasswordCommand(
            changePasswordRequest.OldPassword,
            changePasswordRequest.NewPassword
        );

        await commandBus.Send(command, cancellationToken);

        return Results.NoContent();
    }
}
