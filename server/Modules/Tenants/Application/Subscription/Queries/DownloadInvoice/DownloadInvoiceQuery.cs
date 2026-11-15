using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;

namespace Normora.Modules.Tenants.Application.Subscription.Queries.DownloadInvoice;

public record DownloadInvoiceQuery(Guid? InvoiceId = null) : IRequest<InvoiceDownloadResultDto>;

