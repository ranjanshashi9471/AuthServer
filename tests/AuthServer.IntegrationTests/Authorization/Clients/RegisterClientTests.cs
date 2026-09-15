using System.Net;
using System.Net.Http.Json;
using AuthServer.Contracts.Authorization.Client;
using AuthServer.Infrastructure.Persistence;
using AuthServer.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthServer.IntegrationTests.Authorization.Clients;

public sealed class RegisterClientTests : BaseIntegrationTest
{
    public RegisterClientTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task RegisterClient_ShouldPersistClientAndPreserveExactUriCasing()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var mixedCaseUri = "https://Example.Com/CallBack";
        var request = new CreateClientRequest("TrainBooking Web", "Public", [mixedCaseUri]);

        var response = await Client.PostAsJsonAsync("api/clients", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Deserializing to exact Contract, not dictionary
        var content = await response.Content.ReadFromJsonAsync<CreateClientResponse>();
        content.Should().NotBeNull();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var savedClient = await context
            .Set<Domain.Entities.Client>()
            .Include(c => c.RedirectUris)
            .FirstOrDefaultAsync(c => c.ClientIdentifier == content!.ClientId);

        savedClient.Should().NotBeNull();
        savedClient!.Name.Should().Be("TrainBooking Web");

        savedClient.RedirectUris.Should().HaveCount(1);
        savedClient.RedirectUris.First().Uri.Should().Be(mixedCaseUri); // Exact persistence match
    }

    [Fact]
    public async Task ClientRedirectUri_ShouldEnforceDatabaseUniqueness()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var duplicateUri = "https://app.com/callback";
        var request = new CreateClientRequest("App", "Public", [duplicateUri]);

        var response = await Client.PostAsJsonAsync("api/clients", request);
        var content = await response.Content.ReadFromJsonAsync<CreateClientResponse>();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var savedClient = await context
            .Set<Domain.Entities.Client>()
            .SingleAsync(c => c.ClientIdentifier == content!.ClientId);

        // Bypass aggregate to prove DB-level constraint prevents race conditions
        var act = async () =>
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO client_redirect_uris (\"Id\", \"ClientId\", \"Uri\", \"CreatedAt\", \"UpdatedAt\") VALUES ({Guid.NewGuid()}, {savedClient.Id.Value}, {duplicateUri}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})"
            );
        };

        // 23505 Unique Violation
        var ex = await act.Should().ThrowAsync<Npgsql.PostgresException>();
        ex.Which.SqlState.Should().Be(Npgsql.PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task RegisterClient_Confidential_ShouldSucceedWithZeroRedirectUris()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var request = new CreateClientRequest("Backend Daemon", "Confidential", []);

        var response = await Client.PostAsJsonAsync("api/clients", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<CreateClientResponse>();
        content.Should().NotBeNull();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var savedClient = await context
            .Set<Domain.Entities.Client>()
            .Include(c => c.RedirectUris)
            .FirstOrDefaultAsync(c => c.ClientIdentifier == content!.ClientId);

        savedClient.Should().NotBeNull();
        savedClient!.Type.Should().Be(Domain.Enums.ClientType.Confidential);
        savedClient.RedirectUris.Should().BeEmpty();
    }

    [Fact]
    public async Task Client_ShouldEnforceDatabaseUniqueness_OnClientIdentifier()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var request = new CreateClientRequest("App Primary", "Confidential", []);

        var response = await Client.PostAsJsonAsync("api/clients", request);
        var content = await response.Content.ReadFromJsonAsync<CreateClientResponse>();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        // Attempt duplicate ClientIdentifier insertion directly into DB to prove constraint exists
        var act = async () =>
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO clients (\"Id\", \"ClientIdentifier\", \"Name\", \"Type\", \"RequirePkce\", \"Status\", \"CreatedAt\", \"UpdatedAt\") VALUES ({Guid.NewGuid()}, {content!.ClientId}, 'Duplicate', 0, true, 0, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})"
            );
        };

        var ex = await act.Should().ThrowAsync<Npgsql.PostgresException>();
        ex.Which.SqlState.Should().Be(Npgsql.PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task RegisterClient_ShouldReject_WhenClientTypeIsUndefined()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var request = new CreateClientRequest(
            "TrainBooking",
            "999",
            ["https://localhost/callback"]
        );

        var response = await Client.PostAsJsonAsync("api/clients", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterClient_ShouldThrowConflict_WhenRedirectUriIsDangerous()
    {
        await Client.AuthenticateAsUserWithPermissionsAsync(Factory, "clients.create");
        var request = new CreateClientRequest("App", "Confidential", ["javascript:alert(1)"]);

        var response = await Client.PostAsJsonAsync("api/clients", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409
    }
}
