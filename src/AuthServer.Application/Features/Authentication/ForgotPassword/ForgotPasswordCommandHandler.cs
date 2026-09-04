using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IPasswordResetTokenProvider _tokenProvider;
    private readonly ISecretHasher _secretHasher;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ForgotPasswordCommandHandler(
        IPasswordResetTokenProvider passwordResetTokenProvider,
        ISecretHasher secretHasher,
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService
    )
    {
        _tokenProvider = passwordResetTokenProvider;
        _secretHasher = secretHasher;
        _userRepository = userRepository;
        _tokenRepository = passwordResetTokenRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);

        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null)
            return;

        var tokenData = _tokenProvider.Generate();

        var secretHash = _secretHasher.Hash(tokenData.Secret);

        var resetToken = PasswordResetToken.Create(
            tokenData.Id,
            user.Id,
            secretHash,
            DateTimeOffset.UtcNow.AddMinutes(15)
        );

        await _tokenRepository.AddAsync(resetToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var rawTokenValue = _tokenProvider.BuildToken(tokenData);

        await _notificationService.SendAsync(
            new PasswordResetNotification(user.Email, rawTokenValue, ""),
            cancellationToken
        );
    }
}
