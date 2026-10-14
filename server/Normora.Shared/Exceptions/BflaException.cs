using Normora.Shared.Constants;

namespace Normora.Shared.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to perform an action or access an endpoint they don't have permissions for.
/// This mitigates Broken Function Level Authorization (BFLA) by mapping to a 403 Forbidden.
/// </summary>
public sealed class BflaException(string message = ApiMessages.Forbidden) : Exception(message);
