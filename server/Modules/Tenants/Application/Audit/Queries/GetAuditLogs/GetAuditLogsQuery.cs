using MediatR;
using Normora.Modules.Tenants.Application.Audit.DTOs;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Audit.Queries;

/// <summary>
/// Paginated query for retrieving audit logs with optional filtering by action, free text search, and date range.
/// </summary>
public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Action = null,
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<PagedResult<AuditLogDto>>;

