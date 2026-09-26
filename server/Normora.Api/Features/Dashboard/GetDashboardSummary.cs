using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Modules.Documents.Persistence;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared;

namespace Normora.Api.Features.Dashboard;

public record GetDashboardSummaryQuery() : IRequest<ApiResponse<DashboardSummaryDto>>;

public class GetDashboardSummaryQueryHandler(
    TenantsDbContext tenantsDb,
    DocumentsDbContext documentsDb,
    ConversationsDbContext conversationsDb) : IRequestHandler<GetDashboardSummaryQuery, ApiResponse<DashboardSummaryDto>>
{
    public async Task<ApiResponse<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var thirtyDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-30);

        var totalDocuments = await documentsDb.Documents.CountAsync(cancellationToken);
        var documentsThisMonth = await documentsDb.Documents
            .Where(d => d.UploadedAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var totalEmployees = await tenantsDb.TenantMemberships.CountAsync(cancellationToken);
        var employeesThisMonth = await tenantsDb.TenantMemberships
            .Where(m => m.CreatedAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var totalQuestions = await conversationsDb.Messages.CountAsync(m => m.Role == MessageRole.User, cancellationToken);
        var questionsThisMonth = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User && m.CreatedAt >= thirtyDaysAgoOffset)
            .CountAsync(cancellationToken);

        var recentDocs = await documentsDb.Documents
            .Include(d => d.DocumentDepartments)
            .OrderByDescending(d => d.UploadedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var deptIds = recentDocs.SelectMany(d => d.DocumentDepartments).Select(dd => dd.DepartmentId).Distinct().ToList();
        var departments = await tenantsDb.Departments
            .Where(d => deptIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var recentDocumentsDto = recentDocs.Select(d => new RecentDocumentDto
        {
            Name = d.FileName,
            Category = d.DocumentDepartments.Any() ? string.Join(", ", d.DocumentDepartments.Select(dd => departments.GetValueOrDefault(dd.DepartmentId, "Unknown"))) : "Company Wide",
            Status = d.Status.ToString(),
            Type = System.IO.Path.GetExtension(d.FileName).TrimStart('.').ToUpperInvariant(),
            Date = d.UploadedAt
        }).ToList();

        var sevenDaysAgoOffset = DateTimeOffset.UtcNow.AddDays(-7);
        var recentMessages = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User && m.CreatedAt >= sevenDaysAgoOffset)
            .Select(m => new { m.CreatedAt })
            .ToListAsync(cancellationToken);

        var messagesLast7Days = recentMessages
            .GroupBy(m => m.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var chatActivityDto = new ChatActivityDto
        {
            Labels = new List<string>(),
            Data = new List<int>()
        };

        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            chatActivityDto.Labels.Add(date.ToString("MMM dd"));
            chatActivityDto.Data.Add(messagesLast7Days.GetValueOrDefault(date, 0));
        }

        var topQuestions = await conversationsDb.Messages
            .Where(m => m.Role == MessageRole.User)
            .GroupBy(m => m.Content)
            .Select(g => new TopQuestionDto { Question = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var docTypes = await documentsDb.Documents
            .GroupBy(d => d.FileName.Substring(d.FileName.LastIndexOf(".") + 1))
            .Select(g => new { Extension = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

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

        var totalSavedAnswers = await conversationsDb.SavedAnswers.CountAsync(cancellationToken);
        double answerQuality = totalQuestions > 0 ? Math.Round(((double)totalSavedAnswers / totalQuestions) * 100, 1) : 100.0;
        
        var failedDocs = await documentsDb.Documents.CountAsync(d => d.Status == DocumentStatus.Failed, cancellationToken);
        var processingDocs = await documentsDb.Documents.CountAsync(d => d.Status == DocumentStatus.Processing, cancellationToken);

        var kbDescription = failedDocs > 0 ? $"{failedDocs} documents require attention due to processing errors." : 
                            processingDocs > 0 ? $"{processingDocs} documents are currently being processed." : 
                            "All documents are fully indexed and up to date.";

        var summary = new DashboardSummaryDto
        {
            Stats = new DashboardStatsDto
            {
                TotalDocuments = totalDocuments,
                DocumentsTrend = documentsThisMonth,
                TotalEmployees = totalEmployees,
                EmployeesTrend = employeesThisMonth,
                TotalQuestions = totalQuestions,
                QuestionsTrend = questionsThisMonth,
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

        return ApiResponse<DashboardSummaryDto>.Ok(summary);
    }
}

public class DashboardSummaryDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<DocumentCoverageDto> DocumentCoverage { get; set; } = new();
    public List<RecentDocumentDto> RecentDocuments { get; set; } = new();
    public List<TopQuestionDto> TopQuestions { get; set; } = new();
    public ChatActivityDto ChatActivity { get; set; } = new();
    public KnowledgeBaseReviewDto KbReview { get; set; } = new();
}

public class DashboardStatsDto
{
    public int TotalDocuments { get; set; }
    public int DocumentsTrend { get; set; }
    public int TotalEmployees { get; set; }
    public int EmployeesTrend { get; set; }
    public int TotalQuestions { get; set; }
    public int QuestionsTrend { get; set; }
    public double AnswerQuality { get; set; }
    public double AnswerQualityTrend { get; set; }
}

public class DocumentCoverageDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
    public string HoverColor { get; set; } = string.Empty;
}

public class RecentDocumentDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TopQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ChatActivityDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
}

public class KnowledgeBaseReviewDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
