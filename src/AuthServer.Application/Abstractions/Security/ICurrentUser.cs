using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    UserId? UserId { get; }

    string? Email { get; }
}