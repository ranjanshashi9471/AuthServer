using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AuthServer.UnitTests.Features.Authorization.Clients;

public sealed class ClientTests
{
    [Fact]
    public void CreatePublic_ShouldRequireRedirectUri()
    {
        var act = () => Client.CreatePublic("TrainBooking SPA", []);
        act.Should()
            .Throw<BusinessRuleViolationException>()
            .WithMessage("*at least one redirect URI*");
    }

    [Fact]
    public void CreateConfidential_ShouldSucceed_WithZeroRedirectUris()
    {
        var client = Client.CreateConfidential("Backend API");

        client.RedirectUris.Should().BeEmpty();
        client.Type.Should().Be(ClientType.Confidential);
    }

    [Fact]
    public void CreateConfidential_ShouldAllowAddingRedirectUrisLater()
    {
        var client = Client.CreateConfidential("Backend App");
        client.RedirectUris.Should().BeEmpty();

        client.AddRedirectUri("https://backend.example.com/callback");
        client
            .RedirectUris.Should()
            .ContainSingle(r => r.Uri == "https://backend.example.com/callback");
    }

    [Fact]
    public void AddRedirectUri_ShouldThrowArgumentNullException_WhenUriIsNull()
    {
        var client = Client.CreateConfidential("Backend App");
        var act = () => client.AddRedirectUri(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateConfidential_ShouldRequirePkceByDefault()
    {
        var client = Client.CreateConfidential("Secure API");
        client.RequirePkce.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/path")]
    public void AddRedirectUri_ShouldThrowValidationException_WhenUriIsMalformed(string invalidUri)
    {
        var client = Client.CreateConfidential("Backend App");
        var act = () => client.AddRedirectUri(invalidUri);
        act.Should().Throw<ValidationException>(); // 400
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://app.example.com/callback#token=123")]
    [InlineData("https://user:pass@example.com/callback")]
    [InlineData("http://app.example.com/callback")]
    [InlineData("ftp://example.com/callback")]
    public void AddRedirectUri_ShouldThrowBusinessRuleViolation_WhenUriViolatesPolicy(
        string invalidUri
    )
    {
        var client = Client.CreateConfidential("Backend App");
        var act = () => client.AddRedirectUri(invalidUri);
        act.Should().Throw<BusinessRuleViolationException>(); // 409
    }

    [Theory]
    [InlineData("https://example.com/callback")]
    [InlineData("http://localhost:3000/callback")]
    [InlineData("http://127.0.0.1:5000/callback")]
    [InlineData("http://[::1]:5000/callback")]
    public void AddRedirectUri_ShouldAcceptValidAbsoluteUri(string validUri)
    {
        var client = Client.CreateConfidential("Backend App");
        client.AddRedirectUri(validUri);
        client.RedirectUris.Should().ContainSingle(r => r.Uri == validUri);
    }

    [Fact]
    public void Disable_ShouldTouchUpdatedAt_OnlyWhenStatusChanges()
    {
        var client = Client.CreateConfidential("App");
        var initialUpdate = client.UpdatedAt;

        client.Disable();
        client.Status.Should().Be(ClientStatus.Inactive);
        client.UpdatedAt.Should().BeAfter(initialUpdate);

        var secondUpdate = client.UpdatedAt;
        client.Disable();
        client.UpdatedAt.Should().Be(secondUpdate);
    }
}
