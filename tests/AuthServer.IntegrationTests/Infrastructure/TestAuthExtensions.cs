using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthServer.IntegrationTests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) // Removed IServiceProvider, we don't need the DB here anymore!
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(userIdHeader.ToString(), out var userIdGuid))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid user ID header format."));
        }

        // Give CurrentUser exactly what it expects!
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userIdGuid.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"test_{userIdGuid}@test.com"),
            new Claim(ClaimTypes.NameIdentifier, userIdGuid.ToString()),
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestAuthExtensions
{
    public static async Task<PermissionId> SeedPermissionAsync(
        this IntegrationTestWebAppFactory factory,
        string permissionName
    )
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var existing = await context
            .Set<Permission>()
            .FirstOrDefaultAsync(x => x.Name == permissionName);

        if (existing != null)
        {
            return existing.Id;
        }

        var permission = Permission.Create(permissionName, "Test Permission");
        context.Set<Permission>().Add(permission);
        await context.SaveChangesAsync();

        return permission.Id;
    }

    public static async Task AuthenticateAsUserWithPermissionsAsync(
        this HttpClient client,
        IntegrationTestWebAppFactory factory,
        params string[] permissionNames
    )
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Safely resolve or create permissions
        var permissions = new List<Permission>();
        foreach (var permissionName in permissionNames)
        {
            var permission = await context
                .Set<Permission>()
                .FirstOrDefaultAsync(x => x.Name == permissionName);

            if (permission is null)
            {
                permission = Permission.Create(permissionName, "Test Permission");
                context.Set<Permission>().Add(permission);
            }

            permissions.Add(permission);
        }

        // Assign the actual resolved IDs to the role
        var role = Role.Create("TestRole_" + Guid.NewGuid(), "Test Role");
        foreach (var permission in permissions)
        {
            role.AddPermission(permission.Id);
        }
        context.Set<Role>().Add(role);

        var email = Email.Create($"test_{Guid.NewGuid()}@test.com");
        var username = Username.Create($"user_{Guid.NewGuid()}");
        var passwordHash = PasswordHash.From("DummyHash123!");

        var user = User.Create(email, username, passwordHash);
        user.AssignRole(role.Id);

        context.Set<User>().Add(user);
        await context.SaveChangesAsync();

        client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());
    }
}
