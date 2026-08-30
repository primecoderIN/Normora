using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Normora.Api.Middleware;

/// <summary>
/// A centralized exception handler for the API.
/// It intercepts unhandled exceptions globally and formats them into standard RFC 7807 Problem Details JSON.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle the exception. Returns true if the exception was successfully handled, false otherwise.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Handle ValidationExceptions (usually thrown by MediatR pipeline behaviors)
        // Maps FluentValidation errors directly to a 400 Bad Request response.
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred."
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        // 2. Handle generic unexpected exceptions (e.g., NullReference, DB Connection)
        logger.LogError(exception, "An unhandled exception occurred.");
        
        var serverErrorDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = exception.Message // TODO: In production, consider hiding the actual exception message from the client.
        };
        
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(serverErrorDetails, cancellationToken);
        return true;
    }
}
