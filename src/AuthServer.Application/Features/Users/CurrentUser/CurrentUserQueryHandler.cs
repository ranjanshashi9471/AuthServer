using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.User.CurrentUser;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Application.Features.Users.CurrentUser;

internal sealed class CurrentUserQueryHandler : IQueryHandler<CurrentUserQuery, CurrentUserResponse>
{
    private readonly IUserRepository _user;
    private readonly ICurrentUser _currentUser;

    public CurrentUserQueryHandler(IUserRepository userRepository, ICurrentUser currentUser)
    {
        _currentUser = currentUser;
        _user = userRepository;
    }

    public async Task<CurrentUserResponse> Handle(
        CurrentUserQuery query,
        CancellationToken cancellationToken
    )
    {
        // Roadmap: Credentials/token cannot establish identity -> 401
        var userId =
            _currentUser.UserId ?? throw new AuthenticationException("User is not authenticated.");

        var user = await _user.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            // Roadmap: A requested resource does not exist -> 404
            throw new KeyNotFoundException($"User with ID '{userId.Value}' was not found.");
        }

        return new CurrentUserResponse(user.Id.Value, user.Email.Value, user.Username.Value);
    }
}
