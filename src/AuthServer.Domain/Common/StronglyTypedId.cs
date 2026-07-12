using AuthServer.Domain.Exceptions;

namespace AuthServer.Domain.Common;

public abstract record StronglyTypedId
{
    public Guid Value { get; }

    protected StronglyTypedId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ValidationException("Identifier cannot be empty.");

        Value = value;
    }

    public override string ToString() => Value.ToString();
}