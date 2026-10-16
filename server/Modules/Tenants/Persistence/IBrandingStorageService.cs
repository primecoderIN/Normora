using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// Provides abstractions for interacting with object storage for tenant branding assets.
/// </summary>
public interface IBrandingStorageService
{
    /// <summary>
    /// Uploads a branding asset (e.g., logo or favicon) to the storage service and returns its unique object identifier.
    /// </summary>
    Task<string> UploadAssetAsync(IFormFile file, string tenantSlug, string assetType);

    /// <summary>
    /// Downloads the asset stream and its content type for a given object identifier.
    /// </summary>
    Task<(Stream Content, string ContentType)> DownloadAssetAsync(string objectName);

    /// <summary>
    /// Deletes an asset from object storage.
    /// </summary>
    Task DeleteAssetAsync(string objectName);
}
