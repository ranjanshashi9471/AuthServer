using AuthServer.Api.Exceptions;
using AuthServer.Api.Extensions;
using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Application.DependencyInjection;
using AuthServer.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandling();

builder.Services.AddApplication();

builder.Services.Configure<AuthenticationSecurityOptions>(
    builder.Configuration.GetSection("AuthenticationSecurity")
);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthenticationServices(builder.Configuration);

// 1. Configure Forwarded Headers (Extracts real IP from load balancers/proxies)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// 2. Add Rate Limiting Services
builder.Services.AddAuthRateLimiting();

var app = builder.Build();

// 3. MUST be the very first middleware! This ensures the ExceptionHandler,
// Logger, and RateLimiter all see the real user IP, not the load balancer IP.
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// 4. MUST be after Authorization but before Endpoints.
// This ensures that if you ever rate-limit based on User ID in the future,
// the identity has already been established by the Auth middleware.
app.UseRateLimiter();

app.MapEndpoints();

app.Run();
