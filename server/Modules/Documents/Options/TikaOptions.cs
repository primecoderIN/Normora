using System.ComponentModel.DataAnnotations;

namespace Normora.Modules.Documents.Options;

/// <summary>
/// Configuration for Apache Tika document text extraction service.
/// </summary>
public class TikaOptions
{
    public const string SectionName = "Tika";

    /// <summary>HTTP endpoint of the running Apache Tika server instance.</summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = "http://localhost:9998/";
}
