using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Contracts.Authentication;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.Enums;

namespace AuthServer.Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async Task<LoginResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(
            Email.Create(command.Email),
            cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(
                command.Password,
                user.PasswordHash))
        {
            throw new BusinessRuleViolationException("Invalid email or password.");
        }

        // if (user.Status != UserStatus.Active)
        // {
        //     throw new BusinessRuleViolationException("User not active.");
        // }

        var token = _jwtProvider.GenerateToken(
            new JwtUser(
                user.Id.Value,
                user.Email.Value));

        return new LoginResponse(token);
    }
}