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

    public async Task<CurrentUserResponse> Handle(CurrentUserQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleViolationException("User is not authenticated.");

        var user = await _user.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleViolationException("User not found.");
        }

        return new CurrentUserResponse(user.Id.Value, user.Email.Value, user.Username.Value);
    }
}