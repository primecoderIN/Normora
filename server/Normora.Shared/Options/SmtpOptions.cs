using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = "localhost";
    
    [Range(1, 65535)]
    public int Port { get; set; } = 1025;
    
    [Required]
    [EmailAddress]
    public string From { get; set; } = "noreply@normora.local";
}
