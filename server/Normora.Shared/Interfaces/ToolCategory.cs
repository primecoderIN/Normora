namespace Normora.Shared.Interfaces;

/// <summary>
/// Operational category of an autonomous agent tool, defining its risk level and execution behavior.
/// </summary>
public enum ToolCategory
{
    /// <summary>
    /// Pure query or read operation that alters no persistent workspace state.
    /// Safe for autonomous background execution.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// State-changing mutation (creation, update, dispatch) that modifies workspace data.
    /// Raises workspace data version upon completion to notify clients.
    /// </summary>
    Mutation,

    /// <summary>
    /// Destructive or high-impact operation (permanent deletion, employee removal, permission revoke).
    /// Enforces two-phase human-in-the-loop (HITL) approval with version locking.
    /// </summary>
    Destructive
}

