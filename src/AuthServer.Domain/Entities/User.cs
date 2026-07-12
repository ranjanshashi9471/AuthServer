using AuthServer.Domain.Common;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class User : Entity<UserId>
{
    private User(
        UserId id,
        Email email,
        Username username,
        PasswordHash passwordHash)
        : base(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;

        Status = UserStatus.PendingVerification;
    }

    public Email Email { get; private set; }

    public Username Username { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public UserStatus Status { get; private set; }

    public static User Create(
        Email email,
        Username username,
        PasswordHash passwordHash)
    {
        return new User(
            UserId.New(),
            email,
            username,
            passwordHash);
    }

    public void VerifyEmail()
    {
        if (Status != UserStatus.PendingVerification)
            throw new BusinessRuleViolationException(
                "User is not pending email verification.");

        Status = UserStatus.Active;

        Touch();
    }

    public void Lock()
    {
        if (Status != UserStatus.Active)
            throw new BusinessRuleViolationException(
                "Only active users can be locked.");

        Status = UserStatus.Locked;

        Touch();
    }

    public void Unlock()
    {
        if (Status != UserStatus.Locked)
            throw new BusinessRuleViolationException(
                "User is not locked.");

        Status = UserStatus.Active;

        Touch();
    }

    public void Disable()
    {
        if (Status == UserStatus.Disabled)
            throw new BusinessRuleViolationException(
                $"User '{Id}' is already disabled.");

        Status = UserStatus.Disabled;

        Touch();
    }

    public void ChangePassword(PasswordHash newPasswordHash)
    {
        if (PasswordHash == newPasswordHash)
            throw new BusinessRuleViolationException("New password must be different.");

        PasswordHash = newPasswordHash;

        Touch();
    }


}