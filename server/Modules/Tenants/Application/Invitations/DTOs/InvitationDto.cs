namespace Normora.Modules.Tenants.Application.Invitations.DTOs;

/// <summary>
/// Data transfer object representing the public details of a tenant invitation.
/// </summary>
public record InvitationDto(Guid Token, string Email, string TenantName, string TenantSlug, string Status, DateTime ExpiresAt);

