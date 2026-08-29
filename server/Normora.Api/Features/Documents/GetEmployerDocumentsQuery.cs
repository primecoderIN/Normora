using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Infrastructure;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

public record GetEmployerDocumentsQuery() : IRequest<List<Document>>;

public sealed class GetEmployerDocumentsQueryHandler(AppDbContext context) : IRequestHandler<GetEmployerDocumentsQuery, List<Document>>
{
    public async Task<List<Document>> Handle(GetEmployerDocumentsQuery request, CancellationToken cancellationToken)
    {
        return await context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);
    }
}
