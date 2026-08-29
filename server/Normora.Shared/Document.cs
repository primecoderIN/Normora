using System;

namespace Normora.Shared;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MinioObjectName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string EmployerId { get; set; } = string.Empty;
}
