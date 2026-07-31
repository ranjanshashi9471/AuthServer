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

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork
    )
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
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

        var passwordHash = _passwordHasher.Hash(command.Request.Password);

        var user = User.Create(email, username, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(user.Id.Value);
    }
}
