using FluentValidation;

namespace Normora.Modules.Documents.Application.Documents.Commands;

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before DeleteDocumentCommandHandler executes.
        // Ensures that:
        // - Id: Target document GUID is specified.
        // - TenantId: Ambient workspace context is provided.
        // - DeletedByUserId: Caller user GUID is provided for audit trail stamping.
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.DeletedByUserId).NotEmpty().WithMessage("DeletedByUserId is required.");
    }
}

