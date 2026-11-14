namespace Normora.Shared.Interfaces;

/// <summary>
/// Human-friendly display labels for autonomous tool execution progress chips streamed to the UI.
/// </summary>
/// <param name="StartLabel">Label shown when the tool commences execution (e.g., "Searching workspace documents...").</param>
/// <param name="CompletedLabel">Label shown when execution succeeds (e.g., "Retrieved document excerpts").</param>
/// <param name="FailedLabel">Optional label shown when execution fails (e.g., "Document search failed").</param>
public sealed record ToolChipDescriptor(
    string StartLabel,
    string CompletedLabel,
    string? FailedLabel = null);

