using System.Text;
using AuthServer.Api.Authentication;
using AuthServer.Api.Authorization;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Infrastructure.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Api.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpContextAccessor();

        // 1. Core Scoped Services
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 2. Authorization Handler MUST be Scoped because it injects Scoped services
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // 3. The Policy Provider can be Singleton as it has no scoped dependencies
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        var jwtOptions =
            configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)
                    ),

                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
