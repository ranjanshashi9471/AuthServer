namespace AuthServer.Domain.Exceptions;

public sealed class BusinessRuleViolationException
    : DomainException
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }
}