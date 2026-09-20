using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required]
    public string? ApiKey { get; set; }
    
    [Required]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    
    [Required]
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    
    [Required]
    public string GenerationModel { get; set; } = "gemini-2.0-flash";
}
