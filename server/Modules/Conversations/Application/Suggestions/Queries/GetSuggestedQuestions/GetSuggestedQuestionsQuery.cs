using MediatR;
using Normora.Modules.Conversations.Application.Suggestions.DTOs;

namespace Normora.Modules.Conversations.Application.Suggestions.Queries;

/// <summary>Requests tenant-aware suggested questions for the conversation empty state.</summary>
public record GetSuggestedQuestionsQuery : IRequest<IReadOnlyList<SuggestedQuestionDto>>;

