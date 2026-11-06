namespace Normora.Modules.Tenants.Application.Dashboard.DTOs;

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

