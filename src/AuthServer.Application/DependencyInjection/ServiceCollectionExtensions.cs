using AuthServer.Application.Messaging.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMessaging();
        
        return services;
    }
}