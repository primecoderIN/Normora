using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

/// <summary>
/// Configuration settings for Google Gemini API models, endpoints, and authentication keys.
/// </summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Google AI Studio API key used for generating embeddings and completions.</summary>
    [Required]
    public string? ApiKey { get; set; }
    
    /// <summary>Base URL endpoint for Google Generative Language API.</summary>
    [Required]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    
    /// <summary>Model identifier used for vector embedding generation.</summary>
    [Required]
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    
    /// <summary>Model identifier used for chat completion and reasoning.</summary>
    [Required]
    public string GenerationModel { get; set; } = "gemini-2.0-flash";
}

