using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Normora.Infrastructure;

public interface IDocumentStorageService
{
    Task<string> UploadDocumentAsync(IFormFile file, string employerId);
    Task DeleteDocumentAsync(string objectName);
    Task<string> GetDocumentUrlAsync(string objectName);
}
