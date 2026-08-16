using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Application.Features.Users.ChangePassword;

internal sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork
    )
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (userId is null)
        {
            throw new BusinessRuleViolationException("User is not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleViolationException("User not found.");
        }

        if (!_passwordHasher.Verify(command.OldPassword, user.PasswordHash))
        {
            throw new BusinessRuleViolationException("Current password is incorrect.");
        }

        if (_passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            throw new BusinessRuleViolationException(
                "New password must be different from the current password."
            );
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);

        user.ChangePassword(newPasswordHash);

        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
