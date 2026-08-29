using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Infrastructure;

namespace Normora.Api.Features.Documents;

public record DeleteDocumentCommand(Guid Id, string EmployerId) : IRequest<bool>;

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.EmployerId).NotEmpty().WithMessage("EmployerId is required.");
    }
}

public sealed class DeleteDocumentCommandHandler(AppDbContext context, IDocumentStorageService storageService) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.EmployerId == request.EmployerId, cancellationToken);

        if (document == null)
        {
            return false;
        }

        await storageService.DeleteDocumentAsync(document.MinioObjectName);
            
        context.Documents.Remove(document);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
