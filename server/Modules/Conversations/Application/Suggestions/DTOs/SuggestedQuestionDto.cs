namespace Normora.Modules.Conversations.Application.Suggestions.DTOs;

/// <summary>
/// A single AI-generated or curated question suggestion rendered on the chat empty-state screen.
/// Clicking a card automatically submits the question into the conversation stream.
/// </summary>
/// <param name="Question">The naturally-phrased inquiry text (e.g. "What are the health insurance options?").</param>
/// <param name="IconClass">A PrimeIcons CSS class to render alongside the card (e.g. "pi-file-edit", "pi-shield").</param>
public record SuggestedQuestionDto(string Question, string IconClass);

