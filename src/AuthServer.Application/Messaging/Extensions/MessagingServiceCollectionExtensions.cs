using System.Reflection;
using AuthServer.Application.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Application.Messaging.Extensions;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services)
    {
        RegisterCommandHandlers(
            services,
            Assembly.GetExecutingAssembly());

        services.AddScoped<ICommandBus, Internals.CommandBus>();

        return services;
    }

    private static void RegisterCommandHandlers(
        IServiceCollection services,
        Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (!implementedInterface.IsGenericType) continue;

                if (implementedInterface.GetGenericTypeDefinition() != typeof(ICommandHandler<,>))
                {
                    continue;
                }

                services.AddScoped(
                    implementedInterface,
                    type);
            }
        }
    }
}