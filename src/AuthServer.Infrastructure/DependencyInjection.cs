using AuthServer.Application.Abstractions.Notifications;
using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Infrastructure.Notifications.Development;
using AuthServer.Infrastructure.Notifications.Email;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Persistence.Repositories;
using AuthServer.Infrastructure.Security;
using AuthServer.Infrastructure.Security.EmailVerificationTokens;
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
        services.AddSingleton<IEmailVerificationTokenProvider, EmailVerificationTokenProvider>(); // Added

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>(); // Added

        // Communications / Notifications
        AddNotifications(services, configuration); // Fixed: Properly routes via config

        return services;
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Notifications:Provider");

        if (string.Equals(provider, "Email", StringComparison.OrdinalIgnoreCase))
        {
            // Register Email sender implementation (e.g., SmtpEmailSender, SendGridEmailSender)
            // services.AddScoped<IEmailSender, SmtpEmailSender>();

            services.AddScoped<INotificationService, EmailNotificationService>();
        }
        else
        {
            // Default to Logging for local dev / unconfigured environments
            services.AddScoped<INotificationService, LoggingNotificationService>();
        }
    }
}
