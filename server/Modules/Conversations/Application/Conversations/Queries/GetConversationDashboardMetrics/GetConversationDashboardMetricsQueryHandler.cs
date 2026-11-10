using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Contracts.Conversations;

namespace Normora.Modules.Conversations.Application.Conversations.Queries;

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
        if (request.TenantId == Guid.Empty)
        {
            return new ConversationDashboardMetricsResult(
                0,
                0,
                new Dictionary<DateTime, int>(),
                Array.Empty<TopQuestionData>(),
                0);
        }

        var thirtyDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-30);

        var totalQuestions = await conversationsDb.Messages
            .Where(m => m.TenantId == request.TenantId && m.Role == MessageRole.User)
            .CountAsync(cancellationToken);

        var questionsThisMonth = await conversationsDb.Messages
            .Where(m => m.TenantId == request.TenantId && m.Role == MessageRole.User && m.CreatedAt >= thirtyDaysAgoOffset)
            .CountAsync(cancellationToken);

        var sevenDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-7);
        var recentMessages = await conversationsDb.Messages
            .Where(m => m.TenantId == request.TenantId && m.Role == MessageRole.User && m.CreatedAt >= sevenDaysAgoOffset)
            .Select(m => new { m.CreatedAt })
            .ToListAsync(cancellationToken);

        var messagesLast7Days = recentMessages
            .GroupBy(m => m.CreatedAt.UtcDateTime.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var topQuestionsRaw = await conversationsDb.Messages
            .Where(m => m.TenantId == request.TenantId && m.Role == MessageRole.User && !string.IsNullOrWhiteSpace(m.Content))
            .GroupBy(m => m.Content)
            .Select(g => new { Question = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topQuestions = topQuestionsRaw
            .Select(x => new TopQuestionData(x.Question, x.Count))
            .ToList();

        var totalSavedAnswers = await conversationsDb.SavedAnswers
            .Where(s => s.TenantId == request.TenantId)
            .CountAsync(cancellationToken);

        return new ConversationDashboardMetricsResult(
            totalQuestions,
            questionsThisMonth,
            messagesLast7Days,
            topQuestions,
            totalSavedAnswers);
    }
}

