namespace Normora.Modules.Tenants.Application.Dashboard.DTOs;

/// <summary>
/// Root composite DTO delivering the complete analytics payload for the Employer Analytics Dashboard.
/// Combines multi-module metrics into a unified view for high-level organizational oversight.
/// </summary>
public class DashboardSummaryDto
{
    // High-level KPI metric cards (Total Documents, Active Employees, Total Questions, Answer Quality).
    public DashboardStatsDto Stats { get; set; } = new();

    // Breakdown of document volume categorized by organizational department for donut/pie charts.
    public List<DocumentCoverageDto> DocumentCoverage { get; set; } = new();

    // Feed of recently ingested or updated company documents.
    public List<RecentDocumentDto> RecentDocuments { get; set; } = new();

    // Aggregated ranking of the most frequently asked employee questions.
    public List<TopQuestionDto> TopQuestions { get; set; } = new();

    // 7-day or 30-day time-series data for daily conversational interaction frequency charts.
    public ChatActivityDto ChatActivity { get; set; } = new();

    // AI suggestions or alerts identifying uncovered policy domains needing documentation.
    public KnowledgeBaseReviewDto KbReview { get; set; } = new();
}

/// <summary>
/// Executive KPI summary metrics and week-over-week trend percentages.
/// </summary>
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

/// <summary>
/// Departmental document volume slice for chart visualization.
/// </summary>
public class DocumentCoverageDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
    public string HoverColor { get; set; } = string.Empty;
}

/// <summary>
/// Metadata preview of a recently processed document.
/// </summary>
public class RecentDocumentDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

/// <summary>
/// Frequently asked inquiry pattern with repetition frequency count.
/// </summary>
public class TopQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Time-series chart coordinate points for employee inquiry volume over time.
/// </summary>
public class ChatActivityDto
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
}

/// <summary>
/// Actionable advisory recommendations regarding documentation gaps detected by AI inquiry trends.
/// </summary>
public class KnowledgeBaseReviewDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

