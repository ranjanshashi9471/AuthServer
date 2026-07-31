using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.Logout;

internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenProvider refreshTokenProvider,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork
    )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenProvider = refreshTokenProvider;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Command handler!!");

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

        if (!_passwordHasher.Verify(secret, PasswordHash.From(refreshToken.TokenHash)))
        {
            throw new BusinessRuleViolationException("Invalid refresh token.");
        }

        if (!refreshToken.IsActive)
        {
            return;
        }

        refreshToken.Revoke();

        _refreshTokenRepository.Update(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
