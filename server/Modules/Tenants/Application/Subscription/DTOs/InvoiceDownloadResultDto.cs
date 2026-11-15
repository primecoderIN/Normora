namespace Normora.Modules.Tenants.Application.Subscription.DTOs;

public record InvoiceDownloadResultDto(
    string FileName,
    string ContentType,
    byte[] Content
);

