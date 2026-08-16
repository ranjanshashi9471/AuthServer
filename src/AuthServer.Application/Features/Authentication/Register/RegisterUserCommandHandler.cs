using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;

namespace AuthServer.Application.Features.Authentication.Register;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationTokenProvider _tokenProvider;
    private readonly ISecretHasher _secretHasher;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly INotificationService _notificationService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEmailVerificationTokenProvider tokenProvider,
        ISecretHasher secretHasher,
        IEmailVerificationTokenRepository tokenRepository,
        INotificationService notificationService
    )
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _tokenProvider = tokenProvider;
        _secretHasher = secretHasher;
        _tokenRepository = tokenRepository;
        _notificationService = notificationService;
    }

    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var email = Email.Create(command.Request.Email);
        var username = Username.Create(command.Request.Username);

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                $"A user with email '{email}' already exists."
            );
        }

        // 1. Create the User
        var passwordHash = _passwordHasher.Hash(command.Request.Password);
        var user = User.Create(email, username, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        // 2. Generate the Verification Token
        // Assuming your provider returns an object with Id and Secret (like RefreshTokenProvider)
        // If it just returns a string, adjust accordingly!
        var rawToken = _tokenProvider.Generate();
        var tokenHash = _secretHasher.Hash(rawToken.Secret);

        var verificationToken = EmailVerificationToken.Create(
            rawToken.Id,
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddHours(24) // E.g., token valid for 24 hours
        );

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenString = _tokenProvider.BuildToken(rawToken);

        await _notificationService.SendAsync(
            new EmailVerificationNotification(email, tokenString),
            cancellationToken
        );

        return new RegisterUserResponse(user.Id.Value);
    }
}
