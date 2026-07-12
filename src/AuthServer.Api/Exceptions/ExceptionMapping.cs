using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Exceptions;

internal readonly record struct ExceptionMapping(
    int StatusCode,
    ProblemDetails ProblemDetails);