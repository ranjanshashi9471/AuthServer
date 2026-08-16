using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.ResendVerification;

internal sealed class ResendVerificationCommandHandler : ICommandHandler<ResendVerificationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEmailVerificationTokenProvider _tokenProvider;
    private readonly ISecretHasher _secretHasher;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEmailVerificationTokenProvider tokenProvider,
        ISecretHasher secretHasher,
        INotificationService notificationService,
        IUnitOfWork unitOfWork
    )
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tokenProvider = tokenProvider;
        _secretHasher = secretHasher;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Anti-enumeration: silent return if account does not exist or is already active
        if (user is null || user.Status != UserStatus.PendingVerification)
        {
            return;
        }

        // 1. Invalidate any existing active verification tokens for this user
        await _tokenRepository.InvalidateAllActiveByUserIdAsync(user.Id, cancellationToken);

        // 2. Generate and persist new verification token
        var tokenData = _tokenProvider.Generate();
        var tokenHash = _secretHasher.Hash(tokenData.Secret);

        var verificationToken = EmailVerificationToken.Create(
            tokenData.Id,
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddHours(24)
        );

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Dispatch notification
        var rawToken = _tokenProvider.BuildToken(tokenData);
        await _notificationService.SendAsync(
            new EmailVerificationNotification(user.Email, rawToken),
            cancellationToken
        );
    }
}
