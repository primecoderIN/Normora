using MediatR;
using Microsoft.AspNetCore.Http;
using Normora.Modules.Tenants.Application.Branding.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Branding.Commands;

/// <summary>Updates tenant branding assets and invalidates the cached public branding response.</summary>
public record UpdateTenantBrandingCommand(
    Guid TenantId,
    string? PrimaryColor,
    string? SecondaryColor,
    IFormFile? LogoFile,
    IFormFile? LogoFileDark,
    IFormFile? FaviconFile
) : IRequest<TenantBrandingDto?>, IAuditableRequest
{
    public string Action => "Branding.Updated";
    public string TargetType => "TenantBranding";
    public string? GetTargetId(object? response) => TenantId.ToString();
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { primaryColor = PrimaryColor, secondaryColor = SecondaryColor });
}

