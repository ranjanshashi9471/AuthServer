using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;

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

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ISecretHasher secretHasher, // <-- ADDED THIS
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork,
        IRefreshTokenProvider refreshTokenProvider,
        IRefreshTokenRepository refreshTokenRepository
    )
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _secretHasher = secretHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
        _refreshTokenProvider = refreshTokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _users.GetByEmailAsync(Email.Create(command.Email), cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        // 1. Use PasswordHasher for the user's password
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        // if (user.Status != UserStatus.Active)
        // {
        //     throw new BusinessRuleViolationException("User not active.");
        // }

        var accessToken = _jwtProvider.GenerateToken(new JwtUser(user.Id.Value, user.Email.Value));
        var refreshToken = _refreshTokenProvider.Generate();
        var familyId = RefreshTokenFamilyId.New();

        // 2. Use SecretHasher for the Refresh Token
        var refreshTokenHash = _secretHasher.Hash(refreshToken.Secret);

        var refreshTokenEntity = RefreshToken.Create(
            refreshToken.Id,
            familyId,
            user.Id,
            refreshTokenHash,
            DateTimeOffset.UtcNow.AddDays(30)
        );

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        var refreshTokenValue = _refreshTokenProvider.BuildToken(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshTokenValue);
    }
}
