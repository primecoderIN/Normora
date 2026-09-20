using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class AppOptions
{
    public const string SectionName = "App";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
