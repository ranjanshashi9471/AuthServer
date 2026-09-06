using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace AuthServer.Api.Extensions;

public static class AuthorizationExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        // This tells ASP.NET Core to look for a policy named "users.read".
        // Your PermissionPolicyProvider will catch it and generate the Requirement!
        return builder.RequireAuthorization(new AuthorizeAttribute { Policy = permission });
    }
}
