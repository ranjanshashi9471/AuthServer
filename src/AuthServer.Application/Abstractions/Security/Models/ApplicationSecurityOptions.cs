namespace AuthServer.Application.Abstractions.Security.Models;

public sealed class AuthenticationSecurityOptions
{
    public int MaxFailedAccessAttempts { get; init; } = 5;
    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
}
