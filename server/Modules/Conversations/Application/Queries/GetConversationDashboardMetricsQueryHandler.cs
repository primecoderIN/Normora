using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Contracts.Conversations;

namespace Normora.Modules.Conversations.Application.Queries;

/// <summary>
/// Handles GetConversationDashboardMetricsQuery within the Conversations module boundary.
/// </summary>
public sealed class GetConversationDashboardMetricsQueryHandler(ConversationsDbContext conversationsDb)
    : IRequestHandler<GetConversationDashboardMetricsQuery, ConversationDashboardMetricsResult>
{
    public async Task<ConversationDashboardMetricsResult> Handle(
        GetConversationDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var thirtyDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-30);

        var totalQuestions = await conversationsDb.Messages
            .CountAsync(m => m.Role == MessageRole.User, cancellationToken);

        var questionsThisMonth = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User && m.CreatedAt >= thirtyDaysAgoOffset)
            .CountAsync(cancellationToken);

        var sevenDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-7);
        var recentMessages = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User && m.CreatedAt >= sevenDaysAgoOffset)
            .Select(m => new { m.CreatedAt })
            .ToListAsync(cancellationToken);

        var messagesLast7Days = recentMessages
            .GroupBy(m => m.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var topQuestions = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User)
            .GroupBy(m => m.Content)
            .Select(g => new TopQuestionData(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var totalSavedAnswers = await conversationsDb.SavedAnswers.CountAsync(cancellationToken);

        return new ConversationDashboardMetricsResult(
            totalQuestions,
            questionsThisMonth,
            messagesLast7Days,
            topQuestions,
            totalSavedAnswers);
    }
}
