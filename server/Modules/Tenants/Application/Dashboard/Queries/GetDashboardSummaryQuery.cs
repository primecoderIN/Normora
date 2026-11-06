using MediatR;
using Normora.Modules.Tenants.Application.Dashboard.DTOs;
using Normora.Shared.Contracts.Conversations;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Contracts.Tenants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Dashboard.Queries;

/// <summary>Aggregates dashboard metrics through contracts owned by the respective modules.</summary>
public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler(
    IMediator mediator,
    ITenantContext tenantContext,
    ICacheService cache) : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantGuid = tenantContext.TenantId ?? Guid.Empty;
        var tenantId = tenantContext.TenantId?.ToString() ?? "global";
        var cacheKey = $"dashboard:{tenantId}";

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            // Start independent metrics queries concurrently
            var docMetricsTask = mediator.Send(new GetDocumentDashboardMetricsQuery(tenantGuid), ct);
            var convMetricsTask = mediator.Send(new GetConversationDashboardMetricsQuery(tenantGuid), ct);

            // 1. Await document metrics and resolve department IDs
            var docMetrics = await docMetricsTask;
            var deptIds = docMetrics.RecentDocuments.SelectMany(d => d.DepartmentIds).Distinct().ToList();

            // 2. Resolve department names for recent documents via Tenants contract
            var tenantMetrics = await mediator.Send(new GetTenantDashboardMetricsQuery(tenantGuid, deptIds), ct);

            // 3. Await conversation metrics
            var convMetrics = await convMetricsTask;

            var recentDocumentsDto = docMetrics.RecentDocuments.Select(d => new RecentDocumentDto
            {
                Name = d.FileName,
                Category = d.DepartmentIds.Any()
                    ? string.Join(", ", d.DepartmentIds.Select(depId => tenantMetrics.DepartmentNames.GetValueOrDefault(depId, "Unknown")))
                    : "Company Wide",
                Status = d.Status,
                Type = d.Type,
                Date = d.UploadedAt
            }).ToList();

            var chatActivityDto = new ChatActivityDto
            {
                Labels = new List<string>(),
                Data = new List<int>()
            };

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                chatActivityDto.Labels.Add(date.ToString("MMM dd"));
                chatActivityDto.Data.Add(convMetrics.MessagesLast7Days.GetValueOrDefault(date, 0));
            }

            var topQuestions = convMetrics.TopQuestions.Select(t => new TopQuestionDto
            {
                Question = t.Question,
                Count = t.Count
            }).ToList();

            var docTypes = docMetrics.FileNames
                .GroupBy(f =>
                {
                    var lastDot = f.LastIndexOf('.');
                    return lastDot >= 0 && lastDot < f.Length - 1 ? f.Substring(lastDot + 1) : "";
                })
                .Select(g => new { Extension = g.Key, Count = g.Count() })
                .ToList();

            var colors = new[] { ("#6366f1", "#4f46e5"), ("#3b82f6", "#2563eb"), ("#10b981", "#059669"), ("#f59e0b", "#d97706"), ("#94a3b8", "#64748b") };
            var documentCoverage = docTypes.Select((d, index) => new DocumentCoverageDto
            {
                Label = string.IsNullOrEmpty(d.Extension) ? "UNKNOWN" : d.Extension.ToUpperInvariant(),
                Value = d.Count,
                Color = colors[index % colors.Length].Item1,
                HoverColor = colors[index % colors.Length].Item2
            }).ToList();

            if (!documentCoverage.Any())
            {
                documentCoverage.Add(new DocumentCoverageDto { Label = "No Documents", Value = 1, Color = "#e2e8f0", HoverColor = "#cbd5e1" });
            }

            double answerQuality = convMetrics.TotalQuestions > 0
                ? Math.Round(((double)convMetrics.TotalSavedAnswers / convMetrics.TotalQuestions) * 100, 1)
                : 100.0;

            var kbDescription = docMetrics.FailedDocsCount > 0 ? $"{docMetrics.FailedDocsCount} documents require attention due to processing errors." :
                                docMetrics.ProcessingDocsCount > 0 ? $"{docMetrics.ProcessingDocsCount} documents are currently being processed." :
                                "All documents are fully indexed and up to date.";

            return new DashboardSummaryDto
            {
                Stats = new DashboardStatsDto
                {
                    TotalDocuments = docMetrics.TotalDocuments,
                    DocumentsTrend = docMetrics.DocumentsTrend,
                    TotalEmployees = tenantMetrics.TotalEmployees,
                    EmployeesTrend = tenantMetrics.EmployeesTrend,
                    TotalQuestions = convMetrics.TotalQuestions,
                    QuestionsTrend = convMetrics.QuestionsTrend,
                    AnswerQuality = answerQuality,
                    AnswerQualityTrend = 0.0
                },
                DocumentCoverage = documentCoverage,
                RecentDocuments = recentDocumentsDto,
                TopQuestions = topQuestions,
                ChatActivity = chatActivityDto,
                KbReview = new KnowledgeBaseReviewDto
                {
                    Title = "Knowledge base review",
                    Description = kbDescription
                }
            };
        }, TimeSpan.FromMinutes(2), cancellationToken);
    }
}

