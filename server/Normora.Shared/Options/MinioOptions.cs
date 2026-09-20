using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class MinioOptions
{
    public const string SectionName = "Minio";

    [Required]
    public string Endpoint { get; set; } = "localhost:9000";
    
    [Required]
    public string AccessKey { get; set; } = "admin";
    
    [Required]
    public string SecretKey { get; set; } = "password";
}
