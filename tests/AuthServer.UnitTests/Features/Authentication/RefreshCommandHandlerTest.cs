using System.Reflection;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Features.Authentication.Refresh;
using AuthServer.Contracts.Authentication.Refresh;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthServer.UnitTests.Features.Authentication;

public class RefreshCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ISecretHasher> _secretHasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IRefreshTokenProvider> _refreshTokenProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RefreshCommandHandler _handler;

    public RefreshCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _secretHasherMock = new Mock<ISecretHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _refreshTokenProviderMock = new Mock<IRefreshTokenProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RefreshCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _secretHasherMock.Object,
            _jwtProviderMock.Object,
            _refreshTokenProviderMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowAuthenticationException_WhenParsingFails()
    {
        // Arrange
        var command = new RefreshCommand("invalid.token.string");

        RefreshTokenId dummyId = RefreshTokenId.From(Guid.NewGuid());
        string dummySecret = string.Empty;

        _refreshTokenProviderMock
            .Setup(x => x.TryParse(command.RefreshToken, out dummyId, out dummySecret))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_ShouldTriggerFamilyInvalidation_WhenReusingRotatedToken()
    {
        // Arrange
        var command = new RefreshCommand("valid.token.string");
        var refreshTokenId = RefreshTokenId.From(Guid.NewGuid());
        var secret = "secret_string";

        RefreshTokenId outId = refreshTokenId;
        string outSecret = secret;

        // 1. Setup Parser with strongly typed RefreshTokenId
        _refreshTokenProviderMock
            .Setup(x => x.TryParse(command.RefreshToken, out outId, out outSecret))
            .Returns(true);

        // 2. Create a compromised token (already revoked & rotated)
        var user = CreateActiveUser();
        var compromisedToken = RefreshToken.Create(
            refreshTokenId,
            RefreshTokenFamilyId.New(),
            user.Id,
            "hashed_secret",
            DateTimeOffset.UtcNow.AddDays(30)
        );
        compromisedToken.Revoke(RefreshTokenId.From(Guid.NewGuid())); // Simulate previous rotation

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByIdAsync(refreshTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compromisedToken);

        _secretHasherMock.Setup(x => x.Verify(secret, "hashed_secret")).Returns(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Security violation: Token reuse detected. Session revoked.");

        // PROVE the family invalidation bulk update was triggered
        _refreshTokenRepositoryMock.Verify(
            x => x.RevokeFamilyAsync(compromisedToken.FamilyId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowAuthenticationException_WhenUserIsLockedOut()
    {
        // Arrange
        var command = new RefreshCommand("valid.token.string");
        var refreshTokenId = RefreshTokenId.From(Guid.NewGuid());
        var secret = "secret_string";

        RefreshTokenId outId = refreshTokenId;
        string outSecret = secret;

        _refreshTokenProviderMock
            .Setup(x => x.TryParse(command.RefreshToken, out outId, out outSecret))
            .Returns(true);

        // Create active user and trigger full lockout threshold (5 failed attempts)
        var lockedUser = CreateActiveUser();
        for (var i = 0; i < 5; i++)
        {
            lockedUser.RecordFailedLogin(5, TimeSpan.FromMinutes(15));
        }

        var token = RefreshToken.Create(
            refreshTokenId,
            RefreshTokenFamilyId.New(),
            lockedUser.Id,
            "hashed_secret",
            DateTimeOffset.UtcNow.AddDays(30)
        );

        ForceSetUserNavigation(token, lockedUser);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByIdAsync(refreshTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _secretHasherMock.Setup(x => x.Verify(secret, "hashed_secret")).Returns(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Account is temporarily locked.");
    }

    [Fact]
    public async Task Handle_ShouldSuccessfullyRotateTokens_WhenAllRulesPass()
    {
        // Arrange
        var command = new RefreshCommand("valid.token.string");
        var refreshTokenId = RefreshTokenId.From(Guid.NewGuid());
        var secret = "secret_string";

        RefreshTokenId outId = refreshTokenId;
        string outSecret = secret;

        _refreshTokenProviderMock
            .Setup(x => x.TryParse(command.RefreshToken, out outId, out outSecret))
            .Returns(true);

        var user = CreateActiveUser();
        var token = RefreshToken.Create(
            refreshTokenId,
            RefreshTokenFamilyId.New(),
            user.Id,
            "hashed_secret",
            DateTimeOffset.UtcNow.AddDays(30)
        );
        ForceSetUserNavigation(token, user);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByIdAsync(refreshTokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _secretHasherMock.Setup(x => x.Verify(secret, "hashed_secret")).Returns(true);

        // Setup the new token generation
        var newSecret = new RefreshTokenData(
            RefreshTokenId.From(Guid.NewGuid()),
            "new_secret_string"
        );
        _refreshTokenProviderMock.Setup(x => x.Generate()).Returns(newSecret);
        _secretHasherMock.Setup(x => x.Hash("new_secret_string")).Returns("new_hashed_secret");

        _jwtProviderMock
            .Setup(x => x.GenerateToken(It.IsAny<JwtUser>()))
            .Returns("new_jwt_access_token");
        _refreshTokenProviderMock
            .Setup(x => x.BuildToken(newSecret))
            .Returns("new_refresh_token_string");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_jwt_access_token");
        result.RefreshToken.Should().Be("new_refresh_token_string");

        // Verify state changes
        token.IsRevoked.Should().BeTrue();
        token.WasRotated.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(x => x.Update(token), Times.Once);
        _refreshTokenRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<RefreshToken>(rt => rt.Id == newSecret.Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Helper Methods ---

    private static User CreateActiveUser()
    {
        var user = User.Create(
            Email.Create("test@example.com"),
            Username.Create("testuser"),
            PasswordHash.From("hashed_password")
        );

        user.VerifyEmail(); // Domain transition: PendingVerification -> Active

        return user;
    }

    private void ForceSetUserNavigation(RefreshToken token, User user)
    {
        var property = typeof(RefreshToken).GetProperty(nameof(RefreshToken.User));
        if (property != null && property.CanWrite)
        {
            property.SetValue(token, user, null);
        }
        else
        {
            var backingField = typeof(RefreshToken).GetField(
                "<User>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            backingField?.SetValue(token, user);
        }
    }
}
