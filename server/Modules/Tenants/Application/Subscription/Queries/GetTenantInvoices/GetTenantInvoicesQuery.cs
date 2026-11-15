using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.GetTenantInvoices;

public record GetTenantInvoicesQuery : IRequest<List<TenantInvoiceDto>>;

