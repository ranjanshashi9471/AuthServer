using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.ResendVerification;

internal sealed class ResendVerificationCommandHandler : ICommandHandler<ResendVerificationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEmailVerificationTokenProvider _tokenProvider;
    private readonly ISecretHasher _secretHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    // Constructor formatting fixed
    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEmailVerificationTokenProvider tokenProvider,
        ISecretHasher secretHasher,
        IUnitOfWork unitOfWork,
        INotificationService notificationService
    )
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tokenProvider = tokenProvider;
        _secretHasher = secretHasher;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task Handle(ResendVerificationCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
            return;

        if (user.Status != UserStatus.PendingVerification)
            return;

        // 1. Cooldown Check: Highly optimized DB query fetching ONLY the timestamp
        var latestTokenCreatedAt = await _tokenRepository.GetLatestCreatedAtByUserIdAsync(
            user.Id,
            cancellationToken
        );

        if (latestTokenCreatedAt is not null)
        {
            var elapsed = DateTimeOffset.UtcNow - latestTokenCreatedAt.Value;

            if (elapsed < ResendCooldown)
            {
                // Ceiling prevents UI rounding bugs (e.g., waiting 1.9s showing as 1s)
                var waitSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((ResendCooldown - elapsed).TotalSeconds)
                );

                // Note: Per Codex, we use BusinessRuleViolation for now,
                // but we will want to transition this to a 429 response later.
                throw new BusinessRuleViolationException(
                    $"Please wait {waitSeconds} seconds before requesting another email."
                );
            }
        }

        // 2. Invalidate old tokens
        await _tokenRepository.InvalidateAllActiveByUserIdAsync(user.Id, cancellationToken);

        // 3. Generate and persist new token
        var rawToken = _tokenProvider.Generate();
        var tokenHash = _secretHasher.Hash(rawToken.Secret);

        var verificationToken = EmailVerificationToken.Create(
            rawToken.Id,
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddHours(24)
        );

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);

        // Ensure atomic save before sending email
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Dispatch notification
        var tokenString = _tokenProvider.BuildToken(rawToken);
        await _notificationService.SendAsync(
            new EmailVerificationNotification(user.Email, tokenString),
            cancellationToken
        );
    }
}
