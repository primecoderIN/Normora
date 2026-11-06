using FluentValidation;

namespace Normora.Modules.Documents.Application.Documents.Commands;

public sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.DeletedByUserId).NotEmpty().WithMessage("DeletedByUserId is required.");
    }
}

