using System.Security.Claims;
using AuthServer.Api.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace AuthServer.Api.Endpoints.Users;

public sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/users/me",
            HandleAsync)
        .WithName("GetCurrentUser")
        .WithTags("Users")
        .RequireAuthorization();
    }

    private static IResult HandleAsync(
        ClaimsPrincipal user)
    {
        return Results.Ok(new
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = user.FindFirstValue(ClaimTypes.Email)
        });
    }
}