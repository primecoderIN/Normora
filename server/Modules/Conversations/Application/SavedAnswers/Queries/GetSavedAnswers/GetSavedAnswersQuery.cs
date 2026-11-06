using MediatR;
using Normora.Modules.Conversations.Application.SavedAnswers.DTOs;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Queries;

/// <summary>
/// Returns all saved answers for the current user, ordered newest-first.
/// Includes the message content and source citations for display on the Saved Answers page.
/// </summary>
public record GetSavedAnswersQuery(int Limit = 50, int Offset = 0)
    : IRequest<IReadOnlyList<SavedAnswerDto>>;

