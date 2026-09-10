using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Features.Authentication.Login;
using AuthServer.Contracts.Authentication;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AuthServer.UnitTests.Features.Authentication;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ISecretHasher> _secretHasherMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRefreshTokenProvider> _refreshTokenProviderMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly AuthenticationSecurityOptions _securityOptions;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _secretHasherMock = new Mock<ISecretHasher>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _refreshTokenProviderMock = new Mock<IRefreshTokenProvider>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _securityOptions = new AuthenticationSecurityOptions
        {
            MaxFailedAccessAttempts = 5,
            LockoutDuration = TimeSpan.FromMinutes(15),
        };

        var optionsMock = new Mock<IOptions<AuthenticationSecurityOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_securityOptions);

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _secretHasherMock.Object,
            _jwtProviderMock.Object,
            _unitOfWorkMock.Object,
            _refreshTokenProviderMock.Object,
            _refreshTokenRepositoryMock.Object,
            optionsMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowGenericException_WhenUserDoesNotExist()
    {
        // Arrange: Non-existent users shouldn't expose account existence
        var command = new LoginCommand("ghost@example.com", "Password123!");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_ShouldIncrementFailedCount_WhenPasswordIsIncorrect()
    {
        // Arrange: Wrong password increments AccessFailedCount
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = CreateUser(UserStatus.Active);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, It.IsAny<PasswordHash>()))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");

        user.AccessFailedCount.Should().Be(1);
        user.IsLockedOut.Should().BeFalse();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLockAccount_OnMaxFailedAttempts()
    {
        // Arrange: Fifth failure locks the account
        var command = new LoginCommand("test@example.com", "WrongPassword!");
        var user = CreateUser(UserStatus.Active);

        // Simulate 4 previous failures
        for (int i = 0; i < 4; i++)
        {
            user.RecordFailedLogin(
                _securityOptions.MaxFailedAccessAttempts,
                _securityOptions.LockoutDuration
            );
        }

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, It.IsAny<PasswordHash>()))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");

        user.AccessFailedCount.Should().Be(5);
        user.IsLockedOut.Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Should()
            .BeCloseTo(
                DateTimeOffset.UtcNow.Add(_securityOptions.LockoutDuration),
                TimeSpan.FromSeconds(5)
            );

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRejectImmediately_WithoutExtendingLockout_WhenAccountIsLocked()
    {
        // Arrange: Locked account cannot authenticate, and requests don't extend LockoutEnd
        var command = new LoginCommand("test@example.com", "Password123!");
        var user = CreateUser(UserStatus.Active);

        // Lock the user out 5 minutes ago (10 minutes remaining)
        var originalLockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        ForceLockoutState(user, 5, originalLockoutEnd);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert

        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");

        user.LockoutEnd.Should().Be(originalLockoutEnd);

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldClearLockoutAndAuthenticate_WhenLockoutHasExpired()
    {
        // Arrange: Expired lockout resets correctly on successful login
        var command = new LoginCommand("test@example.com", "CorrectPassword123!");
        var user = CreateUser(UserStatus.Active);

        // Lockout expired 1 minute ago
        ForceLockoutState(user, 5, DateTimeOffset.UtcNow.AddMinutes(-1));

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(command.Password, It.IsAny<PasswordHash>()))
            .Returns(true);

        var tokenData = new RefreshTokenData(RefreshTokenId.New(), "secret");
        _refreshTokenProviderMock.Setup(x => x.Generate()).Returns(tokenData);
        _secretHasherMock.Setup(x => x.Hash("secret")).Returns("hashed_secret");
        _jwtProviderMock
            .Setup(x => x.GenerateToken(It.IsAny<JwtUser>()))
            .Returns("jwt_access_token");
        _refreshTokenProviderMock
            .Setup(x => x.BuildToken(tokenData))
            .Returns("built_refresh_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt_access_token");
        result.RefreshToken.Should().Be("built_refresh_token");

        user.AccessFailedCount.Should().Be(0);
        user.IsLockedOut.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task Handle_ShouldDenyAccess_WhenUserIsDisabled()
    {
        // Arrange: Administrative UserStatus.Disabled remains blocked distinct from temporary lockout
        var command = new LoginCommand("disabled@example.com", "CorrectPassword123!");
        var user = CreateUser(UserStatus.Disabled);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");

        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>()),
            Times.Never
        );
    }

    // --- Helpers ---

    private static User CreateUser(UserStatus status)
    {
        var user = User.Create(
            Email.Create("test@example.com"),
            Username.Create("testuser"),
            PasswordHash.From("hashed_password")
        );

        var statusProperty = typeof(User).GetProperty(nameof(User.Status));
        statusProperty?.SetValue(user, status, null);

        return user;
    }

    private static void ForceLockoutState(User user, int failedAttempts, DateTimeOffset? lockoutEnd)
    {
        var countProperty = typeof(User).GetProperty(nameof(User.AccessFailedCount));
        countProperty?.SetValue(user, failedAttempts, null);

        var endProperty = typeof(User).GetProperty(nameof(User.LockoutEnd));
        endProperty?.SetValue(user, lockoutEnd, null);
    }
}
