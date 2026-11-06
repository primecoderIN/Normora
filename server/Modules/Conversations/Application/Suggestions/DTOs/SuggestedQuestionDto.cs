namespace Normora.Modules.Conversations.Application.Suggestions.DTOs;

/// <summary>
/// A single AI-generated question suggestion for the empty-state of the chat UI.
/// </summary>
/// <param name="Question">The naturally-phrased question string.</param>
/// <param name="IconClass">A PrimeIcons CSS class to render alongside the card (e.g. "pi-file-edit").</param>
public record SuggestedQuestionDto(string Question, string IconClass);

