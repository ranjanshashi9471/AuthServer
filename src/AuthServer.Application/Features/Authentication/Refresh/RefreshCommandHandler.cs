using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Features.Authentication.Refresh;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Refresh;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

internal sealed class RefreshCommandHandler : ICommandHandler<RefreshCommand, RefreshResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISecretHasher _secretHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ISecretHasher secretHasher,
        IJwtProvider jwtProvider,
        IRefreshTokenProvider refreshTokenProvider,
        IUnitOfWork unitOfWork
    )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _secretHasher = secretHasher;
        _jwtProvider = jwtProvider;
        _refreshTokenProvider = refreshTokenProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshResponse> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken
    )
    {
        if (
            !_refreshTokenProvider.TryParse(
                command.RefreshToken,
                out var refreshTokenId,
                out var secret
            )
        )
        {
            throw new BusinessRuleViolationException("Invalid refresh token.");
        }

        var refreshToken = await _refreshTokenRepository.GetByIdAsync(
            refreshTokenId,
            cancellationToken
        );

        if (refreshToken is null)
        {
            throw new BusinessRuleViolationException("Invalid refresh token.");
        }

        if (!refreshToken.IsActive)
        {
            throw new BusinessRuleViolationException("Refresh token has expired or been revoked.");
        }

        if (!_secretHasher.Verify(secret, refreshToken.TokenHash))
        {
            throw new BusinessRuleViolationException("Invalid refresh token.");
        }

        var user = refreshToken.User;

        var newRefreshToken = _refreshTokenProvider.Generate();

        var newRefreshTokenHash = _secretHasher.Hash(newRefreshToken.Secret);

        var newRefreshTokenEntity = RefreshToken.Create(
            newRefreshToken.Id,
            user.Id,
            newRefreshTokenHash,
            DateTimeOffset.UtcNow.AddDays(30)
        );

        refreshToken.Revoke(newRefreshTokenEntity.Id);

        _refreshTokenRepository.Update(refreshToken);

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        var accessToken = _jwtProvider.GenerateToken(new JwtUser(user.Id.Value, user.Email.Value));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshResponse(accessToken, _refreshTokenProvider.BuildToken(newRefreshToken));
    }
}
