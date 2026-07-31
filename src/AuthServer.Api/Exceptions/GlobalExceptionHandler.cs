using Microsoft.AspNetCore.Diagnostics;

namespace AuthServer.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var mapping = ExceptionToProblemDetailsMapper.Map(exception);

        httpContext.Response.StatusCode = mapping.StatusCode;

        await httpContext.Response.WriteAsJsonAsync(mapping.ProblemDetails, cancellationToken);

        return true;
    }
}
