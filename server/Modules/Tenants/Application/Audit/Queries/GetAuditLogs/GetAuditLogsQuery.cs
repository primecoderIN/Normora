using MediatR;
using Normora.Modules.Tenants.Application.Audit.DTOs;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Audit.Queries;

/// <summary>
/// Paginated query for retrieving audit logs with optional filtering by action, free text search, and date range.
/// Handled by <see cref="GetAuditLogsQueryHandler"/> to provide compliance reporting for enterprise admins.
/// </summary>
/// <remarks>
/// Security Workflow:
/// - Enforces ambient TenantId filtering via ITenantContext to prevent cross-tenant audit leakage (BOLA protection).
/// - Restricts execution strictly to tenant owners and designated administrators (BFLA authorization).
/// - Default pagination (Page 1, PageSize 20, capped at 100) prevents denial-of-service memory pressure.
/// </remarks>
public record GetAuditLogsQuery(
    // Current 1-based page index.
    int Page = 1,

    // Page window size (clamped between 1 and 100 in handler).
    int PageSize = 20,

    // Optional exact-match action filter (e.g. "EmployeeInvited", "DocumentDeleted").
    string? Action = null,

    // Case-insensitive substring query matching actor email, name, or target identifiers.
    string? Search = null,

    // Optional start boundary for time-window audits (inclusive UTC).
    DateTime? FromDate = null,

    // Optional end boundary for time-window audits (inclusive UTC).
    DateTime? ToDate = null
) : IRequest<PagedResult<AuditLogDto>>;

