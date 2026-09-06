using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// Provides abstractions for interacting with the underlying object storage system (e.g., MinIO/S3).
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Uploads a document to the storage service and returns its unique object identifier.
    /// </summary>
    Task<string> UploadDocumentAsync(IFormFile file, string employerId);

    /// <summary>
    /// Downloads the document stream for a given object identifier.
    /// </summary>
    Task<Stream> DownloadDocumentAsync(string objectName);

    /// <summary>
    /// Deletes a document from object storage.
    /// </summary>
    Task DeleteDocumentAsync(string objectName);

    /// <summary>
    /// Generates a pre-signed URL or direct URL for retrieving the document.
    /// </summary>
    Task<string> GetDocumentUrlAsync(string objectName);
}
