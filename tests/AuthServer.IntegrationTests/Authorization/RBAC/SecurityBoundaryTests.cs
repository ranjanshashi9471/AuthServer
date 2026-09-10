using System.Net;
using System.Net.Http.Json;
using AuthServer.Api.Endpoints.Roles;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Infrastructure.Persistence;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthServer.IntegrationTests.Authorization.RBAC;

public sealed class SecurityBoundaryTests : BaseIntegrationTest
{
    public SecurityBoundaryTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task CreateRole_Should_Forbid_Privilege_Escalation()
    {
        // 1. Arrange - A user who can CREATE roles, but cannot DELETE users
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var createRolesPerm = Permission.Create(Permissions.RolesCreate, "Create roles");
        var deleteUsersPerm = Permission.Create(Permissions.UsersDelete, "Delete users (DANGER)");
        context.Set<Permission>().AddRange(createRolesPerm, deleteUsersPerm);

        var limitedAdminRole = Role.Create("LimitedAdmin", "Can make basic roles");
        limitedAdminRole.AddPermission(createRolesPerm.Id);
        context.Set<Role>().Add(limitedAdminRole);

        var attacker = User.Create(
            Email.Create("attacker@test.com"),
            Username.Create("attacker"),
            PasswordHash.From("Dummy123!")
        );
        attacker.AssignRole(limitedAdminRole.Id);
        context.Set<User>().Add(attacker);

        await context.SaveChangesAsync();

        // 2. Act - Attacker tries to create a role with a permission they don't have
        Client.DefaultRequestHeaders.Add("X-Test-UserId", attacker.Id.Value.ToString());

        var maliciousRequest = new CreateRoleRequest(
            "GodRole",
            "I shouldn't be able to make this",
            [deleteUsersPerm.Id.Value] // Attempting to grant UsersDelete
        );

        var response = await Client.PostAsJsonAsync("api/roles", maliciousRequest);

        // 3. Assert - The system MUST reject this to prevent privilege escalation
        // Depending on your API design, this should be 403 Forbidden or 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_Should_Forbid_Privilege_Escalation()
    {
        // 1. Arrange - A user who can ASSIGN roles, but cannot DELETE users
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var assignRolesPerm = Permission.Create(
            Permissions.UsersAssignRole,
            "Assign roles to users"
        );
        var deleteUsersPerm = Permission.Create(Permissions.UsersDelete, "Delete users (DANGER)");
        context.Set<Permission>().AddRange(assignRolesPerm, deleteUsersPerm);

        // The attacker's current role
        var limitedAdminRole = Role.Create("LimitedAdmin", "Can assign basic roles");
        limitedAdminRole.AddPermission(assignRolesPerm.Id);
        context.Set<Role>().Add(limitedAdminRole);

        // The target role the attacker wants to steal
        var superAdminRole = Role.Create("SuperAdmin", "God mode");
        superAdminRole.AddPermission(deleteUsersPerm.Id); // Has a permission the attacker lacks
        context.Set<Role>().Add(superAdminRole);

        var attacker = User.Create(
            Email.Create("attacker2@test.com"),
            Username.Create("attacker2"),
            PasswordHash.From("Dummy123!")
        );
        attacker.AssignRole(limitedAdminRole.Id);
        context.Set<User>().Add(attacker);

        await context.SaveChangesAsync();

        // 2. Act - Attacker tries to assign themselves the SuperAdmin role
        Client.DefaultRequestHeaders.Add("X-Test-UserId", attacker.Id.Value.ToString());

        var response = await Client.PostAsync(
            $"api/users/{attacker.Id.Value}/roles/{superAdminRole.Id.Value}",
            null
        );

        // 3. Assert - The system MUST reject this to prevent privilege escalation
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
