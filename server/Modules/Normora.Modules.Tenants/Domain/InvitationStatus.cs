namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Represents the lifecycle state of a tenant invitation.
/// Using an enum instead of raw strings eliminates typo risks and enables exhaustive switch matching.
/// </summary>
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2
}
