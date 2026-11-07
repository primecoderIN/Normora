using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Normora.Shared;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using System.Reflection;

namespace Normora.Api.Middleware;

/// <summary>
/// A centralized exception handler for the API.
/// It intercepts unhandled exceptions globally and formats them into standard RFC 7807 Problem Details JSON.
/// In production, it securely masks internal framework/infrastructure errors while faithfully returning
/// sanitized business messages defined in <see cref="ApiMessages"/>.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env) : IExceptionHandler
{
    /// <summary>
    /// Reflection-cached set of all declared constant messages in <see cref="ApiMessages"/>.
    /// Used in production to verify whether an exception message is a deliberate, safe business rule message
    /// versus an arbitrary framework leak (e.g. EF Core internal message).
    /// </summary>
    private static readonly HashSet<string> SafeDomainMessages = typeof(ApiMessages)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetValue(null)!)
        .ToHashSet(StringComparer.Ordinal);

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

            var response = ApiResponse<Dictionary<string, string[]>>.Failure(ApiMessages.ValidationFailed, errors);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }

        // 2. Handle domain business rule violations (e.g. duplicate slug, invalid invite state).
        // Maps to 400 Bad Request — the client sent a logically invalid request.
        // Known constants from ApiMessages are safely propagated even in production so the client UI
        // receives actionable feedback. Unknown internal framework exceptions fall back to ApiMessages.BadRequest.
        if (exception is InvalidOperationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var message = env.IsProduction() && !SafeDomainMessages.Contains(exception.Message)
                ? ApiMessages.BadRequest
                : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 3. Handle resource-not-found errors thrown from command/query handlers.
        // Maps to 404 Not Found. Also handles BOLA (Broken Object Level Authorization) by returning generic 404 to prevent enumeration.
        if (exception is BolaException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(ApiMessages.NotFound), cancellationToken);
            return true;
        }

        if (exception is NotFoundException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            var message = env.IsProduction() && !SafeDomainMessages.Contains(exception.Message)
                ? ApiMessages.NotFound
                : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 4. Handle authentication failures thrown from command handlers.
        // Maps to 401 Unauthorized.
        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var message = env.IsProduction() && !SafeDomainMessages.Contains(exception.Message)
                ? ApiMessages.Unauthorized
                : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 5. Handle authorization failures (BFLA).
        // Maps to 403 Forbidden.
        if (exception is BflaException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            var message = env.IsProduction() && !SafeDomainMessages.Contains(exception.Message)
                ? ApiMessages.Forbidden
                : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 6. Handle subscription plan limit exceeded errors.
        // Maps to 402 Payment Required.
        if (exception is PlanLimitExceededException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            var message = env.IsProduction() && !SafeDomainMessages.Contains(exception.Message)
                ? ApiMessages.PlanLimitExceeded
                : exception.Message;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(message), cancellationToken);
            return true;
        }

        // 7. Handle generic unexpected exceptions (e.g. database failure, unhandled runtime errors)
        logger.LogError(exception, "An unhandled exception occurred.");
        
        var serverErrorMessage = env.IsProduction() ? ApiMessages.InternalServerError : exception.Message;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse.Failure(serverErrorMessage), cancellationToken);
        return true;
    }
}
