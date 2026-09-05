using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Normora.Modules.Documents.Persistence;

public interface IDocumentStorageService
{
    Task<string> UploadDocumentAsync(IFormFile file, string employerId);
    Task<Stream> DownloadDocumentAsync(string objectName);
    Task DeleteDocumentAsync(string objectName);
    Task<string> GetDocumentUrlAsync(string objectName);
}
