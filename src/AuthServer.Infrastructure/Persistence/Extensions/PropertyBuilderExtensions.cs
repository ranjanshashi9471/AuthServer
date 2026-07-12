using System.Linq.Expressions;
using AuthServer.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Extensions;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TId> HasStronglyTypedIdConversion<TId>(
        this PropertyBuilder<TId> builder,
        Expression<Func<Guid, TId>> fromProvider)
        where TId : StronglyTypedId
    {
        builder.HasConversion(
            id => id.Value,
            fromProvider);

        return builder;
    }

    public static PropertyBuilder<TValueObject> HasValueObjectConversion<TValueObject>(
        this PropertyBuilder<TValueObject> builder,
        Expression<Func<TValueObject, string>> toProvider,
        Expression<Func<string, TValueObject>> fromProvider)
    {
        builder.HasConversion(
            toProvider,
            fromProvider);

        return builder;
    }
}