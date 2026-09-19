using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Normora.Shared;

namespace Normora.Api.Middleware;

/// <summary>
/// A centralized exception handler for the API.
/// It intercepts unhandled exceptions globally and formats them into standard RFC 7807 Problem Details JSON.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env) : IExceptionHandler
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

            var response = ApiResponse<Dictionary<string, string[]>>.Failure("One or more validation errors occurred.", errors);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }

        // 2. Handle domain business rule violations (e.g. duplicate slug, invalid invite state).
        // Maps to 400 Bad Request — the client sent a logically invalid request.
        // Detail is hidden in production to avoid leaking internal state.
        if (exception is InvalidOperationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var message = env.IsProduction() ? "The request could not be processed." : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 3. Handle authentication/authorization failures thrown from command handlers.
        // Maps to 401 Unauthorized.
        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(exception.Message), cancellationToken);
            return true;
        }

        // 4. Handle generic unexpected exceptions (e.g., NullReference, DB Connection)
        logger.LogError(exception, "An unhandled exception occurred.");
        
        var serverErrorMessage = env.IsProduction() ? "An unexpected error occurred. Please try again later." : exception.Message;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(serverErrorMessage), cancellationToken);
        return true;
    }
}
