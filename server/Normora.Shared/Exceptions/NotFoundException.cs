namespace Normora.Shared.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist or the current user is not authorized to access it.
/// The <see cref="GlobalExceptionHandler"/> maps this to <c>HTTP 404 Not Found</c>.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);
