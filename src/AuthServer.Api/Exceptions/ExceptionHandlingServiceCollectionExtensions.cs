namespace AuthServer.Api.Exceptions;

public static class ExceptionHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
