using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Contracts.Authentication;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.Entities;
using AuthServer.Application.Abstractions.Security.Models;

namespace AuthServer.Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IUnitOfWork unitOfWork,
        IRefreshTokenProvider refreshTokenProvider,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _unitOfWork = unitOfWork;
        _refreshTokenProvider = refreshTokenProvider;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(
            Email.Create(command.Email),
            cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(
                command.Password,
                user.PasswordHash))
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        // if (user.Status != UserStatus.Active)
        // {
        //     throw new BusinessRuleViolationException("User not active.");
        // }

        var accessToken = _jwtProvider.GenerateToken(
            new JwtUser(
                user.Id.Value,
                user.Email.Value));

        var refreshToken = _refreshTokenProvider.Generate();

        var refreshTokenHash =
            _passwordHasher.Hash(refreshToken.Secret);

        var refreshTokenEntity =
            RefreshToken.Create(
                refreshToken.Id,
                user.Id,
                refreshTokenHash.Value,
                DateTimeOffset.UtcNow.AddDays(30));

        await _refreshTokenRepository.AddAsync(
            refreshTokenEntity,
            cancellationToken);

        var refreshTokenValue =
            _refreshTokenProvider.BuildToken(refreshToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);



        return new LoginResponse(
            accessToken,
            refreshTokenValue);
    }
}