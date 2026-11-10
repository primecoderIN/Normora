using FluentValidation;
using Microsoft.AspNetCore.Http;
using Normora.Shared.Constants;

namespace Normora.Modules.Documents.Application.Documents.Commands;

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UploadDocumentCommandHandler executes.
        // Guards against malicious payloads, oversized files, and MIME spoofing attacks before disk/storage writes.
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(ApiMessages.TenantIdRequired);

        RuleFor(x => x.File)
            .NotNull().WithMessage(ApiMessages.FileRequired)
            .Must(file => file?.Length > 0).WithMessage(ApiMessages.FileEmpty)
            .Must(file => file?.Length <= 20_971_520).WithMessage(ApiMessages.FileExceedsLimit)
            .Must(BeAValidExtension).WithMessage(ApiMessages.InvalidFileType)
            .Must(HaveValidMagicBytes).WithMessage(ApiMessages.InvalidFileContent);
    }

    private static bool BeAValidExtension(IFormFile? file)
    {
        // Extension Allowlist:
        // Strictly permits supported document formats (.pdf, .docx, .txt) and image formats (.png, .jpg, .jpeg, .webp)
        // supported by the Gemini multimodal text extraction and OCR engine.
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext is ".pdf" or ".docx" or ".txt" or ".png" or ".jpg" or ".jpeg" or ".webp";
    }

    /// <summary>
    /// Validates the actual file content against known magic byte signatures.
    /// Prevents attackers from renaming a malicious file (e.g. .exe) to a trusted extension.
    /// </summary>
    private static bool HaveValidMagicBytes(IFormFile? file)
    {
        // Deep File Inspection (Magic Byte Signature Verification):
        // Prevents extension-spoofing attacks (e.g., an executable malicious binary renamed to 'handbook.pdf').
        // Uses stackalloc byte[12] for zero heap allocation during stream inspection.
        if (file == null || file.Length < 4) return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Plain text (.txt) files have no standard magic byte header
        if (ext == ".txt") return true;

        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read < 4) return false;

        return ext switch
        {
            // PDF Magic Header: "%PDF" (0x25 0x50 0x44 0x46)
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            // DOCX / OOXML Magic Header: PK ZIP Archive (0x50 0x4B 0x03 0x04)
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            // PNG Magic Header: 0x89 0x50 0x4E 0x47 (\x89PNG)
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            // JPEG Magic Header: 0xFF 0xD8 0xFF
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            // WEBP Magic Header: "RIFF" .... "WEBP"
            ".webp" => read >= 12 &&
                       header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                       header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }
}

