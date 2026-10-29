using MediatR;

namespace Normora.Shared.Contracts.Conversations;

/// <summary>
/// Encapsulates a top frequent question asked by employees in the workspace.
/// </summary>
public sealed record TopQuestionData(string Question, int Count);

/// <summary>
/// Encapsulates chat activity, message trends, top inquiries, and answer metrics for the dashboard.
/// </summary>
public sealed record ConversationDashboardMetricsResult(
    int TotalQuestions,
    int QuestionsTrend,
    IReadOnlyDictionary<DateTime, int> MessagesLast7Days,
    IReadOnlyList<TopQuestionData> TopQuestions,
    int TotalSavedAnswers);

/// <summary>
/// Cross-module query to retrieve conversation metrics and inquiry trends without leaking ConversationsDbContext.
/// </summary>
public sealed record GetConversationDashboardMetricsQuery(Guid TenantId) : IRequest<ConversationDashboardMetricsResult>;
