using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Models;

namespace Normora.Modules.Tenants.Application.Audit;

/// <summary>
/// Data transfer object representing a historical tenant security/audit record.
/// </summary>
public record AuditLogDto(
    Guid Id,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorName,
    string Action,
    string? TargetType,
    string? TargetId,
    string? Metadata,
    DateTime OccurredAt
);

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

/// <summary>
/// Handles retrieving tenant audit records with date range normalizations and text searching.
/// </summary>
public class GetAuditLogsQueryHandler(
    TenantsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Enforce tenant boundary: audit logs are strictly isolated to the calling tenant
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException(ApiMessages.TenantContextMissing);

        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        // Filter by action type (e.g. USER_INVITED, DOCUMENT_UPLOADED)
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var actionFilter = request.Action.Trim();
            query = query.Where(a => a.Action == actionFilter);
        }

        // Substring search across actor email, display name, target entity ID, and metadata
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(a =>
                (a.ActorEmail != null && a.ActorEmail.ToLower().Contains(search)) ||
                (a.ActorName != null && a.ActorName.ToLower().Contains(search)) ||
                (a.TargetId != null && a.TargetId.ToLower().Contains(search)) ||
                (a.Metadata != null && a.Metadata.ToLower().Contains(search)));
        }

        // Apply UTC-normalized date boundaries
        if (request.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.OccurredAt >= fromUtc);
        }

        if (request.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.OccurredAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.ActorUserId,
                a.ActorEmail,
                a.ActorName,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.Metadata,
                a.OccurredAt))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize, totalPages);
    }
}

