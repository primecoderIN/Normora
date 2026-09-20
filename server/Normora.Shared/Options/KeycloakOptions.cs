using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    public string Authority { get; set; } = default!;
    
    [Required]
    public string MetadataAddress { get; set; } = default!;
    
    // Default mapped values for Docker internal routing vs external browser access
    [Required]
    public string InternalAuthority { get; set; } = "http://keycloak:8080";
    
    [Required]
    public string ExternalAuthority { get; set; } = "http://localhost:8080";
}
