using System.ComponentModel.DataAnnotations;

namespace Normora.Shared.Options;

public class TikaOptions
{
    public const string SectionName = "Tika";

    [Required]
    [Url]
    public string Endpoint { get; set; } = "http://localhost:9998/";
}
