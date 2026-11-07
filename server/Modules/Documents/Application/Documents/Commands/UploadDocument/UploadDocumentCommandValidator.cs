using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Normora.Modules.Documents.Application.Documents.Commands;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UploadDocumentCommandHandler executes.
        // Guards against malicious payloads, oversized files, and MIME spoofing attacks before disk/storage writes.
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
        // Extension Allowlist:
        // Strictly permits only supported enterprise document formats (.pdf, .docx, .txt)
        // supported by Apache Tika and the semantic text chunking engine.
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
        // Deep File Inspection (Magic Byte Signature Verification):
        // Prevents extension-spoofing attacks (e.g., an executable malicious binary renamed to 'handbook.pdf').
        // Uses stackalloc byte[8] for zero heap allocation during stream inspection.
        if (file == null || file.Length < 4) return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Plain text (.txt) files have no standard magic byte header
        if (ext == ".txt") return true;

        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 4) return false;

        return ext switch
        {
            // PDF Magic Header: "%PDF" (0x25 0x50 0x44 0x46)
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            // DOCX / OOXML Magic Header: PK ZIP Archive (0x50 0x4B 0x03 0x04)
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
    }
}

