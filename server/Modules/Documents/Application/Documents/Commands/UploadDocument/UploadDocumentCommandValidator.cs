using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Normora.Modules.Documents.Application.Documents.Commands;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("No file was uploaded.")
            .Must(file => file?.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file?.Length <= 20_971_520).WithMessage("File exceeds 20MB limit.")
            .Must(BeAValidExtension).WithMessage("Only .pdf, .docx, and .txt files are allowed.")
            .Must(HaveValidMagicBytes).WithMessage("File content does not match the declared file type.");
    }

    private static bool BeAValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext == ".pdf" || ext == ".docx" || ext == ".txt";
    }

    /// <summary>
    /// Validates the actual file content against known magic byte signatures.
    /// Prevents attackers from renaming a malicious file (e.g. .exe) to a trusted extension.
    /// </summary>
    private static bool HaveValidMagicBytes(IFormFile? file)
    {
        if (file == null || file.Length < 4) return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // TXT has no standard magic bytes — trust the extension
        if (ext == ".txt") return true;

        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 4) return false;

        return ext switch
        {
            // PDF: starts with %PDF (0x25 0x50 0x44 0x46)
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            // DOCX is a ZIP archive: starts with PK (0x50 0x4B 0x03 0x04)
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
    }
}

