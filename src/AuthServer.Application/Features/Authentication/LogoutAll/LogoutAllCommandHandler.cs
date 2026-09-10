using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authentication.LogoutAll;

internal sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutAllCommandHandler(
        ICurrentUser currentUser,
        IRefreshTokenRepository refreshTokenRepository
    )
    {
        _currentUser = currentUser;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        var userId =
            _currentUser.UserId ?? throw new AuthenticationException("User is not authenticated.");

        // Instantly revokes every active token for this user in a single database round-trip
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
    }
}
