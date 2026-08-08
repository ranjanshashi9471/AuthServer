using AuthServer.Application.Abstractions.Communication.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Infrastructure.Communication.Notifications;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Persistence.Repositories;
using AuthServer.Infrastructure.Security;
using AuthServer.Infrastructure.Security.Jwt;
using AuthServer.Infrastructure.Security.PasswordResetTokens;
using AuthServer.Infrastructure.Security.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found."
            );

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AuthDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Security / Token Providers
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecretHasher, SecretHasher>();
        services.AddSingleton<IRefreshTokenProvider, RefreshTokenProvider>();
        services.AddSingleton<IPasswordResetTokenProvider, PasswordResetTokenProvider>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Communications / Notifications
        services.AddTransient<INotificationService, EmailNotificationService>();
        return services;
    }
}
