using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

/// <summary>
/// Returns all saved answers for the current user, ordered newest-first.
/// Includes the message content and source citations for display on the Saved Answers page.
/// </summary>
public record GetSavedAnswersQuery(int Limit = 50, int Offset = 0)
    : IRequest<IReadOnlyList<SavedAnswerDto>>;

public class GetSavedAnswersQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetSavedAnswersQuery, IReadOnlyList<SavedAnswerDto>>
{
    public async Task<IReadOnlyList<SavedAnswerDto>> Handle(
        GetSavedAnswersQuery request, CancellationToken cancellationToken)
    {
        return await context.SavedAnswers
            .AsNoTracking()
            .Where(s => s.UserId == currentUser.KeycloakUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(s => new SavedAnswerDto(
                s.Id,
                s.MessageId,
                s.ConversationId,
                s.Message.Content,
                s.Message.Citations
                    .Select(c => new CitationDto(c.DocumentId, c.FileName, c.Score))
                    .ToList(),
                s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
