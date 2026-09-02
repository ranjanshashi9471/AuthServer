using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.Extensions.Options;

namespace AuthServer.Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecretHasher _secretHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthenticationSecurityOptions _securityOptions;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ISecretHasher secretHasher,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork,
        IRefreshTokenProvider refreshTokenProvider,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<AuthenticationSecurityOptions> securityOptions // Inject configuration
    )
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _secretHasher = secretHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
        _refreshTokenProvider = refreshTokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _securityOptions = securityOptions.Value;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _users.GetByEmailAsync(Email.Create(command.Email), cancellationToken);

        if (user is null)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (user.Status == UserStatus.Locked || user.Status == UserStatus.Disabled)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (user.Status == UserStatus.PendingVerification)
        {
            throw new BusinessRuleViolationException(
                "Please verify your email address before logging in."
            );
        }

        // 4. Temporary Security Lockout Evaluation (Phase 7C)
        user.ClearExpiredLockout();

        if (user.IsLockedOut)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        // 5. Verify Password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            // Record the failure, persist to DB, and throw generic error.
            user.RecordFailedLogin(
                _securityOptions.MaxFailedAccessAttempts,
                _securityOptions.LockoutDuration
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Invalid email or password.");
        }

        // 6. Success! Clear failures and issue tokens.
        user.RecordSuccessfulLogin();

        var accessToken = _jwtProvider.GenerateToken(new JwtUser(user.Id.Value, user.Email.Value));
        var refreshToken = _refreshTokenProvider.Generate();
        var familyId = RefreshTokenFamilyId.New();
        var refreshTokenHash = _secretHasher.Hash(refreshToken.Secret);

        var refreshTokenEntity = RefreshToken.Create(
            refreshToken.Id,
            familyId,
            user.Id,
            refreshTokenHash,
            DateTimeOffset.UtcNow.AddDays(30)
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        // 7. Save successful login state and new tokens to DB atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshTokenValue = _refreshTokenProvider.BuildToken(refreshToken);

        return new LoginResponse(accessToken, refreshTokenValue);
    }
}
