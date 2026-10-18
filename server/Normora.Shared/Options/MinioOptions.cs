using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

/// <summary>
/// Connection options for MinIO S3-compatible document storage.
/// </summary>
public class MinioOptions
{
    public const string SectionName = "Minio";

    /// <summary>MinIO host and port endpoint (e.g. localhost:9000).</summary>
    [Required]
    public string Endpoint { get; set; } = "localhost:9000";
    
    /// <summary>Access key ID for S3 API authentication.</summary>
    [Required]
    public string AccessKey { get; set; } = "admin";
    
    /// <summary>Secret access key for S3 API authentication.</summary>
    [Required]
    public string SecretKey { get; set; } = "password";
}

