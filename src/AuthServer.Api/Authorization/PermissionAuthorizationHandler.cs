using AuthServer.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace AuthServer.Api.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        ICurrentUser currentUser
    )
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return;
        }

        var permissions = await _permissionService.GetPermissionsAsync(
            userId,
            context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None
        );

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
