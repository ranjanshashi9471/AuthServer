using AuthServer.Application.Abstractions.Communication.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IPasswordResetTokenProvider _tokenProvider;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly ISecretHasher _secretHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenProvider passwordResetTokenProvider,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ISecretHasher secretHasher,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher
    )
    {
        _tokenProvider = passwordResetTokenProvider;
        _resetTokenRepository = passwordResetTokenRepository;
        _secretHasher = secretHasher;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (
            !_tokenProvider.TryParse(
                command.AccessToken,
                out var resetPasswordTokenId,
                out var secret
            )
        )
        {
            throw new BusinessRuleViolationException("Invalid Token");
        }

        var resetToken = await _resetTokenRepository.GetByIdAsync(
            resetPasswordTokenId,
            cancellationToken
        );

        if (resetToken is null)
            throw new BusinessRuleViolationException("Invalid Token");

        if (resetToken.IsUsed || resetToken.IsExpired)
            throw new BusinessRuleViolationException(
                "Reset token has expired or already been used."
            );

        if (!_secretHasher.Verify(secret, resetToken.TokenHash))
            throw new BusinessRuleViolationException("Invalid or expired reset token.");

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);

        if (user is null)
            throw new BusinessRuleViolationException("User not found.");

        if (_passwordHasher.Verify(command.NewPassword, user.PasswordHash))
            throw new BusinessRuleViolationException(
                "New password must be different from current password."
            );

        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newPasswordHash);

        resetToken.Use();

        var activeRefreshTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(
            user.Id,
            cancellationToken
        );

        foreach (var token in activeRefreshTokens)
        {
            token.Revoke();
        }

        // 10. Commit changes atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
