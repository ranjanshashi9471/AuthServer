using AuthServer.Api.Exceptions;
using AuthServer.Application.DependencyInjection;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authentication;
using AuthServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandling();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var handler = scope.ServiceProvider.GetService<
        ICommandHandler<RegisterUserCommand, RegisterUserResponse>>();

    Console.WriteLine(handler is not null
        ? "Handler registered successfully."
        : "Handler registration failed.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();