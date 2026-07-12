using AuthServer.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Exceptions;

internal static class ExceptionToProblemDetailsMapper
{
    public static ExceptionMapping Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => new ExceptionMapping(
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = validationException.Message
                }),

            BusinessRuleViolationException businessException => new ExceptionMapping(
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Business Rule Violation",
                    Detail = businessException.Message
                }),

            _ => new ExceptionMapping(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred."
                })
        };
    }
}