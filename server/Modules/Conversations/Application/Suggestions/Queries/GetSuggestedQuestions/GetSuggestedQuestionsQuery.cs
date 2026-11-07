using MediatR;
using Normora.Modules.Conversations.Application.Suggestions.DTOs;

namespace Normora.Modules.Conversations.Application.Suggestions.Queries;

/// <summary>
/// Query to generate or retrieve contextual starter questions for the conversational chat empty state.
/// Handled by <see cref="GetSuggestedQuestionsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Contextual Suggestions:
/// - Analyzes the document inventory accessible to the caller within their ambient tenant and departments.
/// - Returns 3-4 natural questions (e.g. "What is our remote work stipend?", "How do I request parental leave?")
///   to onboard employees and demonstrate AI capabilities on first open.
/// </remarks>
public record GetSuggestedQuestionsQuery : IRequest<IReadOnlyList<SuggestedQuestionDto>>;

