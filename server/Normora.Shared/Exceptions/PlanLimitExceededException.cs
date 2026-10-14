namespace Normora.Shared.Exceptions;

/// <summary>
/// Thrown when an operation exceeds the tenant's subscription plan quota (e.g. max documents, members, storage).
/// Maps globally to HTTP 402 Payment Required.
/// </summary>
public class PlanLimitExceededException(string message) : Exception(message)
{
}
