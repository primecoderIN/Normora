using MediatR;
using Normora.Modules.Conversations.Application.SavedAnswers.DTOs;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Queries;

/// <summary>
/// Query to retrieve the chronological collection of saved answers bookmarked by the authenticated employee.
/// Handled by <see cref="GetSavedAnswersQueryHandler"/>.
/// </summary>
/// <remarks>
/// Scoping & Ordering:
/// - Filtered strictly by ambient TenantId and caller's ExternalUserId.
/// - Ordered newest-first by CreatedAt timestamp.
/// - Includes message markdown bodies and grounded document citation lists.
/// </remarks>
public record GetSavedAnswersQuery(int Limit = 50, int Offset = 0)
    : IRequest<IReadOnlyList<SavedAnswerDto>>;

