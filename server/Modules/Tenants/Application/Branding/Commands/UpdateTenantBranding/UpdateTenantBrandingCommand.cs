using MediatR;
using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Branding.Commands;

/// <summary>
/// Command to update white-label branding assets (hex color codes, light/dark logos, favicons) for a tenant.
/// Handled by <see cref="UpdateTenantBrandingCommandHandler"/>.
/// </summary>
/// <remarks>
/// Security & Lifecycle Workflow:
/// - Verifies caller has Tenant Owner or Admin role for the target TenantId (BFLA check).
/// - Validates file streams: strictly inspects magic bytes (PNG/SVG/JPEG/ICO) and caps file sizes at 2MB.
/// - Stores approved binary assets in MinIO under deterministic object paths ({tenantId}/branding/...).
/// - Invalidates public memory cache entries so updated themes propagate instantly across active sessions.
/// - Implements <see cref="IAuditableRequest"/> to record changes in the tenant compliance audit trail.
/// </remarks>
public record UpdateTenantBrandingCommand(
    // The target organization's unique identifier.
    Guid TenantId,

    // Optional primary branding color in standard CSS hex notation (e.g. "#4F46E5").
    string? PrimaryColor,

    // Optional secondary accent color in standard CSS hex notation.
    string? SecondaryColor,

    // Multipart form file representing the primary light-mode organization logo.
    IFormFile? LogoFile,

    // Multipart form file representing the dark-mode optimized logo variant.
    IFormFile? LogoFileDark,

    // Multipart form file representing the browser tab favicon (.ico or .png).
    IFormFile? FaviconFile
) : IRequest<TenantBrandingDto?>, IAuditableRequest
{
    // Standard audit event identifier dispatched via MediatR AuditBehavior.
    public string Action => "Branding.Updated";

    // Target domain entity type for compliance reporting.
    public string TargetType => "TenantBranding";

    // Returns the TenantId as the affected target entity identifier.
    public string? GetTargetId(object? response) => TenantId.ToString();

    // Serializes non-binary configuration changes into the audit log metadata.
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { primaryColor = PrimaryColor, secondaryColor = SecondaryColor });
}

