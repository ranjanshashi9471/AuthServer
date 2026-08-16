using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Application.Features.Authentication.VerifyEmail;

internal sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand>
{
    private const string InvalidTokenMessage = "Invalid email verification token.";

    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly ISecretHasher _secretHasher;
    private readonly IEmailVerificationTokenProvider _tokenProvider;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository tokenRepository,
        ISecretHasher secretHasher,
        IEmailVerificationTokenProvider tokenProvider,
        IUnitOfWork unitOfWork
    )
    {
        _tokenRepository = tokenRepository;
        _secretHasher = secretHasher;
        _tokenProvider = tokenProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        if (!_tokenProvider.TryParse(command.Token, out var tokenId, out var secret))
        {
            throw new AuthenticationException(InvalidTokenMessage);
        }

        var token = await _tokenRepository.GetByIdAsync(tokenId, cancellationToken);

        if (token is null || !token.IsActive)
        {
            throw new AuthenticationException(InvalidTokenMessage);
        }

        if (!_secretHasher.Verify(secret, token.TokenHash))
        {
            throw new AuthenticationException(InvalidTokenMessage);
        }

        var user = token.User;

        // Ensure the token isn't effectively stale because the user is already verified
        if (user.Status != UserStatus.PendingVerification)
        {
            throw new AuthenticationException(InvalidTokenMessage);
        }

        user.VerifyEmail();
        token.Use();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
