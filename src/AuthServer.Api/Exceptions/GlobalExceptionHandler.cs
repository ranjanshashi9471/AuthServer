using Microsoft.AspNetCore.Diagnostics;

namespace AuthServer.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var mapping = ExceptionToProblemDetailsMapper.Map(exception);

        if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled error occurred: {Message}",
                exception.Message
            );
        }
        else
        {
            _logger.LogWarning(
                "A domain/application exception occurred ({StatusCode}): {Message}",
                mapping.StatusCode,
                exception.Message
            );
        }

        httpContext.Response.StatusCode = mapping.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(mapping.ProblemDetails, cancellationToken);

        return true;
    }
}
