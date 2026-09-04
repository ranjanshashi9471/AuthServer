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
        // 1. Parsing & cryptographic validation
        if (
            !_tokenProvider.TryParse(
                command.AccessToken,
                out var resetPasswordTokenId,
                out var secret
            )
        )
        {
            throw new AuthenticationException("Invalid or expired reset token.");
        }

        var resetToken = await _resetTokenRepository.GetByIdAsync(
            resetPasswordTokenId,
            cancellationToken
        );

        // Early fast-path advisory check
        if (resetToken is null || !resetToken.IsActive)
        {
            throw new AuthenticationException("Invalid or expired reset token.");
        }

        if (!_secretHasher.Verify(secret, resetToken.TokenHash))
        {
            throw new AuthenticationException("Invalid or expired reset token.");
        }

        // 2. Uniform error handling for user resolution (Anti-enumeration)
        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);

        if (user is null)
        {
            throw new AuthenticationException("Invalid or expired reset token.");
        }

        // 3. Prevent reusing the current password
        if (_passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            throw new BusinessRuleViolationException(
                "New password must be different from the current password."
            );
        }

        // 4. Atomic execution & domain state transitions
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // Concurrency Boundary: Atomic check-and-set in DB guarantees single-use
        var consumed = await _resetTokenRepository.MarkAsUsedAsync(
            resetToken.Id,
            cancellationToken
        );

        if (!consumed)
        {
            throw new AuthenticationException("Invalid or expired reset token.");
        }

        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newPasswordHash);

        // Reset failed login counter and clear temporary lockout
        user.RecordSuccessfulLogin();

        // Invalidate all active sessions globally
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        // Persist User state changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 5. Post-commit notification dispatch
        // await _notificationService.SendAsync(
        //     new SecurityAlertNotification(user.Email, "Your password was recently changed."),
        //     cancellationToken
        // );
    }
}
