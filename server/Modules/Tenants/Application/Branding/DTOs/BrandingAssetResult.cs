namespace Normora.Modules.Tenants.Application.Branding.DTOs;

/// <summary>
/// Encapsulates a resolved binary branding asset stream (logo/favicon) and its verified MIME content type.
/// Streamed directly to HTTP clients by TenantsController to serve images without exposing private MinIO bucket credentials.
/// </summary>
public sealed record BrandingAssetResult(
    // The readable binary stream retrieved from MinIO storage.
    Stream Stream,

    // Verified MIME type (e.g. "image/png", "image/svg+xml", "image/x-icon").
    string ContentType
);

