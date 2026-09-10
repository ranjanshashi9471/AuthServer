using System.Net;
using System.Net.Http.Json;
using AuthServer.Api.Endpoints.Roles;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Infrastructure.Persistence;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthServer.IntegrationTests.Authorization.RBAC;

public sealed class RbacEvaluationTests : BaseIntegrationTest
{
    public RbacEvaluationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Should_Grant_Access_When_Permission_Exists_In_Secondary_Role()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Create Role 1 (Irrelevant Permission)
        var role1 = Role.Create("Role1", "Basic Role");
        var perm1 = Permission.Create(Permissions.UsersRead, "Read Users");
        context.Set<Permission>().Add(perm1);
        role1.AddPermission(perm1.Id);
        context.Set<Role>().Add(role1);

        // Create Role 2 (Required Permission)
        var role2 = Role.Create("Role2", "Admin Role");
        var perm2 = Permission.Create(Permissions.RolesCreate, "Create Roles");
        context.Set<Permission>().Add(perm2);
        role2.AddPermission(perm2.Id);
        context.Set<Role>().Add(role2);

        // Create User and assign BOTH roles
        var user = User.Create(
            Email.Create("multi_role@test.com"),
            Username.Create("multirole"),
            PasswordHash.From("DummyHash123!")
        );
        user.AssignRole(role1.Id);
        user.AssignRole(role2.Id);
        context.Set<User>().Add(user);

        await context.SaveChangesAsync();

        // 2. Act
        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());
        var request = new CreateRoleRequest("NewRole", "Desc", []);
        var response = await Client.PostAsJsonAsync("api/roles", request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Should_Deny_Access_When_User_Has_No_Roles()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = User.Create(
            Email.Create("noroles@test.com"),
            Username.Create("noroles"),
            PasswordHash.From("DummyHash123!")
        );
        context.Set<User>().Add(user);
        await context.SaveChangesAsync();

        // 2. Act
        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());
        var request = new CreateRoleRequest("NewRole", "Desc", []);
        var response = await Client.PostAsJsonAsync("api/roles", request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Accumulate_Distinct_Permissions_When_Multiple_Roles_Overlap()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Shared permission
        var permission = Permission.Create(Permissions.RolesCreate, "Create Roles");
        context.Set<Permission>().Add(permission);

        // Role A gets the permission
        var roleA = Role.Create("RoleA", "Desc");
        roleA.AddPermission(permission.Id);
        context.Set<Role>().Add(roleA);

        // Role B gets the EXACT SAME permission
        var roleB = Role.Create("RoleB", "Desc");
        roleB.AddPermission(permission.Id);
        context.Set<Role>().Add(roleB);

        // User gets BOTH roles
        var user = User.Create(
            Email.Create("overlap@test.com"),
            Username.Create("overlap"),
            PasswordHash.From("Dummy123!")
        );
        user.AssignRole(roleA.Id);
        user.AssignRole(roleB.Id);
        context.Set<User>().Add(user);

        await context.SaveChangesAsync();

        // 2. Act
        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());
        var request = new CreateRoleRequest("TestRoleOverlap", "Desc", []);
        var response = await Client.PostAsJsonAsync("api/roles", request);

        // 3. Assert - Should succeed and NOT fail due to sequence duplication errors, proving .Distinct() works.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Should_Deny_Access_Immediately_When_Permission_Is_Revoked()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var permission = Permission.Create(Permissions.RolesCreate, "Create Roles");
        context.Set<Permission>().Add(permission);

        var role = Role.Create("RevokeRole", "Desc");
        role.AddPermission(permission.Id);
        context.Set<Role>().Add(role);

        var user = User.Create(
            Email.Create("revoke@test.com"),
            Username.Create("revoke"),
            PasswordHash.From("Dummy123!")
        );
        user.AssignRole(role.Id);
        context.Set<User>().Add(user);

        await context.SaveChangesAsync();

        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());

        // 2. Act 1 - Initial request should SUCCEED
        var request1 = new CreateRoleRequest("RoleBeforeRevoke", "Desc", []);
        var response1 = await Client.PostAsJsonAsync("api/roles", request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Revoke the permission directly in the database
        // (If your Role entity has a .RemovePermission method, use that instead. We'll drop the join record directly here for certainty)
        var rolePermission = await context
            .Set<RolePermission>()
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

        context.Set<RolePermission>().Remove(rolePermission!);
        await context.SaveChangesAsync(); // <-- Permission is gone.

        // 4. Act 2 - Same user, same headers, new request. Should FAIL immediately.
        var request2 = new CreateRoleRequest("RoleAfterRevoke", "Desc", []);
        var response2 = await Client.PostAsJsonAsync("api/roles", request2);

        // 5. Assert
        response2.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Deny_Access_When_Role_Has_Zero_Permissions()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Create a role but add NO permissions to it
        var role = Role.Create("EmptyRole", "No permissions here");
        context.Set<Role>().Add(role);

        var user = User.Create(
            Email.Create("empty@test.com"),
            Username.Create("emptyrole"),
            PasswordHash.From("Dummy123!")
        );
        user.AssignRole(role.Id);
        context.Set<User>().Add(user);

        await context.SaveChangesAsync();

        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.Value.ToString());

        // 2. Act
        var request = new CreateRoleRequest("NewRole", "Desc", []);
        var response = await Client.PostAsJsonAsync("api/roles", request);

        // 3. Assert - Having a role isn't enough; they need the specific permission
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Be_Idempotent_When_Duplicate_Role_Is_Assigned()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var role = Role.Create("DupRole", "Desc");
        context.Set<Role>().Add(role);

        var user = User.Create(
            Email.Create("dup@test.com"),
            Username.Create("duprole"),
            PasswordHash.From("Dummy123!")
        );

        // 2. Act - Attempt to assign the exact same role twice
        user.AssignRole(role.Id);
        user.AssignRole(role.Id);

        context.Set<User>().Add(user);

        // If the Domain entity (User.AssignRole) doesn't use a HashSet or check for existing roles,
        // this SaveChangesAsync will throw a DbUpdateException (PK violation).
        await context.SaveChangesAsync();

        // 3. Assert - Only one record should actually exist in the DB
        var userRolesCount = await context
            .Set<UserRole>()
            .CountAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

        userRolesCount.Should().Be(1);
    }
}
