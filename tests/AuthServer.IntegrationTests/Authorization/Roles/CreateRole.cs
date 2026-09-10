using System.Net;
using System.Net.Http.Json;
using AuthServer.Contracts.Authorization.Role;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AuthServer.IntegrationTests.Authorization.Roles;

public sealed class CreateRoleTests : BaseIntegrationTest
{
    public CreateRoleTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task CreateRole_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        var request = new CreateRoleRequest("Admin", "Full access", []);

        var response = await Client.PostAsJsonAsync("api/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRole_ShouldReturnForbidden_WhenUserLacksRolesCreatePermission()
    {
        var request = new CreateRoleRequest("Admin", "Full access", []);

        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "users.read");

        var response = await Client.PostAsJsonAsync("api/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRole_ShouldReturnCreated_WhenUserHasRequiredPermission()
    {
        var existingPermissionId = await Factory.SeedPermissionAsync("users.read");

        var request = new CreateRoleRequest(
            "Admin",
            "Administrator Role",
            [existingPermissionId.Value]
        );

        // FIX: Give the user the permission they are trying to assign!
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "roles.create", "users.read");

        var response = await Client.PostAsJsonAsync("api/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.ToString().Should().Contain("/api/roles/");
    }

    [Fact]
    public async Task CreateRole_ShouldReturnNotFound_WhenPermissionIdsDoNotExist()
    {
        var request = new CreateRoleRequest("Admin", "Administrator Role", [Guid.NewGuid()]);

        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "roles.create");

        var response = await Client.PostAsJsonAsync("api/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
