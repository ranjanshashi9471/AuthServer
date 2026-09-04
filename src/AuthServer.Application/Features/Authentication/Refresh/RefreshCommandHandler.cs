using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.Features.Authentication.Refresh;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication.Refresh;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Application.Features.Authentication.Refresh;

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
            throw new AuthenticationException("Invalid refresh token.");
        }

        var refreshToken = await _refreshTokenRepository.GetByIdAsync(
            refreshTokenId,
            cancellationToken
        );

        // 1. Verify token exists and hash matches
        if (refreshToken is null || !_secretHasher.Verify(secret, refreshToken.TokenHash))
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        // 2. Standard expiration check
        if (refreshToken.IsExpired)
        {
            throw new AuthenticationException("Refresh token has expired. Please log in again.");
        }

        // 3. TOKEN REUSE DETECTION (Family Invalidation)
        if (refreshToken.IsRevoked)
        {
            if (refreshToken.WasRotated)
            {
                // Security Breach: Someone is reusing a rotated token!
                await _refreshTokenRepository.RevokeFamilyAsync(
                    refreshToken.FamilyId,
                    cancellationToken
                );

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new AuthenticationException(
                    "Security violation: Token reuse detected. Session revoked."
                );
            }

            throw new AuthenticationException("Refresh token has been revoked.");
        }

        var user = refreshToken.User;

        // 4. CRITICAL: Enforce User Status and Lockouts
        if (user.Status != UserStatus.Active)
        {
            throw new AuthenticationException("Account is not active.");
        }

        user.ClearExpiredLockout();

        if (user.IsLockedOut)
        {
            throw new AuthenticationException("Account is temporarily locked.");
        }

        // 5. Rotate the tokens
        var newRefreshToken = _refreshTokenProvider.Generate();
        var newRefreshTokenHash = _secretHasher.Hash(newRefreshToken.Secret);

        var newRefreshTokenEntity = RefreshToken.Create(
            newRefreshToken.Id,
            refreshToken.FamilyId,
            user.Id,
            newRefreshTokenHash,
            DateTimeOffset.UtcNow.AddDays(30)
        );

        refreshToken.Revoke(newRefreshTokenEntity.Id);

        // Explicit update is fine, though EF Core tracks it automatically.
        await _refreshTokenRepository.Update(refreshToken);
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        var accessToken = _jwtProvider.GenerateToken(new JwtUser(user.Id.Value, user.Email.Value));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshResponse(accessToken, _refreshTokenProvider.BuildToken(newRefreshToken));
    }
}
