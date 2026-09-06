using Microsoft.AspNetCore.Authorization;

namespace AuthServer.Api.Authorization;

public static class PermissionAuthorizationExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(new AuthorizeAttribute { Policy = permission });

        return builder;
    }
}
