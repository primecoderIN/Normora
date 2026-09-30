namespace Normora.Shared.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to access a specific resource they don't own or have access to.
/// This mitigates Broken Object Level Authorization (BOLA) by mapping to a 404 Not Found, preventing enumeration.
/// </summary>
public sealed class BolaException(string message = "The requested resource was not found.") : Exception(message);
