using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Api.Authentication;

internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public UserId? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(value, out var id))
            {
                return null;
            }

            return UserId.From(id);
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(
            JwtRegisteredClaimNames.Email);
}