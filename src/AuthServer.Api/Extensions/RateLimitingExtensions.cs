using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Extensions;

public static class RateLimitingExtensions
{
    // Refined Policy Names
    public const string Register = "auth_register";
    public const string Login = "auth_login";
    public const string VerifyEmail = "auth_verify_email";
    public const string ResendVerification = "auth_resend_verification";
    public const string ForgotPassword = "auth_forgot_password";
    public const string ResetPassword = "auth_reset_password";
    public const string Refresh = "auth_refresh";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Customizing the rejection response to match our Global API Standard (ProblemDetails)
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // If the limiter tells us when the window resets, pass it to the client!
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = (
                        (int)retryAfter.TotalSeconds
                    ).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "You have exceeded the rate limit. Please try again later.",
                    },
                    cancellationToken
                );
            };

            // 1. Register: Protects CPU from password hashing exhaustion (3/min)
            options.AddPolicy(Register, ctx => BuildFixedWindow(ctx, 3));

            // 2. Login: Protects against basic brute force (5/min)
            options.AddPolicy(Login, ctx => BuildFixedWindow(ctx, 5));

            // 3. Verify Email: General abuse protection (5/min)
            options.AddPolicy(VerifyEmail, ctx => BuildFixedWindow(ctx, 5));

            // 4. Resend Verification: Protects Email Provider Costs (3/min)
            options.AddPolicy(ResendVerification, ctx => BuildFixedWindow(ctx, 3));

            // 5. Forgot Password: Anti-Spam (3/min)
            options.AddPolicy(ForgotPassword, ctx => BuildFixedWindow(ctx, 3));

            // 6. Reset Password: Anti-Spam (5/min)
            options.AddPolicy(ResetPassword, ctx => BuildFixedWindow(ctx, 5));

            // 7. Refresh: Looser limit, protects DB from runaway frontend loops (15/min)
            options.AddPolicy(Refresh, ctx => BuildFixedWindow(ctx, 15));
        });

        return services;
    }

    // Helper method to keep policy definitions clean
    private static RateLimitPartition<string> BuildFixedWindow(HttpContext context, int permitLimit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetIpAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0, // Instant drop
            }
        );
    }

    private static string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? context.TraceIdentifier;
    }
}
