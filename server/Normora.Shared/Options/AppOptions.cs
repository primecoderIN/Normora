using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

/// <summary>
/// Core application routing and base domain settings.
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Base frontend application URL used for generating invitation links and CORS.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:4200";
}

