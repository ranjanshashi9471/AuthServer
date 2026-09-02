using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions; // Can be removed if we use silent returns

namespace AuthServer.Application.Features.Authentication.Logout;

internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly ISecretHasher _secretHasher;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenProvider refreshTokenProvider,
        ISecretHasher secretHasher,
        IUnitOfWork unitOfWork
    )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenProvider = refreshTokenProvider;
        _secretHasher = secretHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // 1. If parsing fails, silently succeed
        if (
            !_refreshTokenProvider.TryParse(
                command.RefreshToken,
                out var refreshTokenId,
                out var secret
            )
        )
        {
            return;
        }

        var refreshToken = await _refreshTokenRepository.GetByIdAsync(
            refreshTokenId,
            cancellationToken
        );

        // 2. If it doesn't exist, silently succeed
        if (refreshToken is null)
        {
            return;
        }

        // 3. If the secret is wrong, silently succeed
        if (!_secretHasher.Verify(secret, refreshToken.TokenHash))
        {
            return;
        }

        // 4. Defense-in-depth: Revoke the entire family via the optimized bulk update
        await _refreshTokenRepository.RevokeFamilyAsync(refreshToken.FamilyId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
