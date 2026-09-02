using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;

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
        // 1. Token format verification -> 401
        if (
            !_tokenProvider.TryParse(
                command.AccessToken,
                out var resetPasswordTokenId,
                out var secret
            )
        )
            throw new AuthenticationException("Invalid or expired reset token.");

        var resetToken = await _resetTokenRepository.GetByIdAsync(
            resetPasswordTokenId,
            cancellationToken
        );

        if (resetToken is null)
            throw new AuthenticationException("Invalid or expired reset token.");

        if (resetToken.IsUsed || resetToken.IsExpired)
            throw new AuthenticationException("Reset token has expired or already been used.");

        if (!_secretHasher.Verify(secret, resetToken.TokenHash))
            throw new AuthenticationException("Invalid or expired reset token.");

        // 2. User resolution -> 404
        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User not found.");

        // 3. Business logic validation -> 400/409
        if (_passwordHasher.Verify(command.NewPassword, user.PasswordHash))
            throw new BusinessRuleViolationException(
                "New password must be different from the current password."
            );

        // 4. Begin Explicit Transaction for mixed execution strategies
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);

        user.ChangePassword(newPasswordHash);
        resetToken.Use();

        // High-performance immediate database execution
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        // Save in-memory tracked changes (User, ResetToken)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Commit the transaction
        await transaction.CommitAsync(cancellationToken);

        // 5. Dispatch Security Notification
        // MUST happen after the transaction commits to avoid sending false alerts!
        // await _notificationService.SendAsync(
        //     new SecurityAlertNotification(
        //         user.Email,
        //         "Your password was recently changed. If you did not make this request, please contact support immediately."
        //     ),
        //     cancellationToken
        // );
    }
}
