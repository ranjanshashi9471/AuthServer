using AuthServer.Api.Abstractions;
using AuthServer.Application.Features.Authorization.Clients.CreateClient;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authorization.Client;
using AuthServer.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthServer.Api.Endpoints.Authorization.Clients;

public sealed class RegisterClient : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/clients",
                async (
                    CreateClientRequest request,
                    ICommandBus commandBus,
                    CancellationToken cancellationToken
                ) =>
                {
                    if (
                        !Enum.TryParse<ClientType>(request.Type, true, out var clientType)
                        || !Enum.IsDefined(clientType)
                    )
                    {
                        return Results.BadRequest(
                            new { Error = "Invalid or unsupported client type." }
                        );
                    }

                    var command = new CreateClientCommand(
                        request.Name,
                        clientType,
                        request.RedirectUris ?? []
                    );
                    var response = await commandBus.Send(command, cancellationToken);

                    // Treat as a command operation returning 200 OK since no GET endpoint exists to form a 201 Location.
                    return Results.Ok(response);
                }
            )
            .WithTags("Clients")
            .RequireAuthorization(
                AuthServer.Application.Abstractions.Security.Permissions.ClientsCreate
            );
    }
}
