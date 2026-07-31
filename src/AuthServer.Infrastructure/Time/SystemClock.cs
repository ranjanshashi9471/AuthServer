using AuthServer.Application.Abstractions.Time;

namespace AuthServer.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
