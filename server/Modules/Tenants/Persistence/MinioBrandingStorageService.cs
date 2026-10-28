using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Normora.Modules.Tenants.Persistence;

/// <summary>
/// A storage service implementation that interacts with a MinIO S3-compatible object storage server.
/// MinIO is used to physically store the tenant branding assets.
/// </summary>
public class MinioBrandingStorageService(IMinioClient minioClient, ILogger<MinioBrandingStorageService> logger) : IBrandingStorageService
{
    private const string BucketName = "normora-branding";
    private static volatile bool _bucketEnsured;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Checks if the configured bucket exists in MinIO lazily on first upload, and creates it if it doesn't.
    /// Thread-safe and non-blocking during DI service instantiation.
    /// </summary>
    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketEnsured) return;
        await _lock.WaitAsync();
        try
        {
            if (_bucketEnsured) return;
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
            if (!found)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
            }
            _bucketEnsured = true;
        }
        catch (MinioException e)
        {
            logger.LogError(e, "Error ensuring bucket {BucketName} exists in MinIO", BucketName);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Streams a newly uploaded file directly into MinIO storage.
    /// </summary>
    /// <param name="file">The uploaded file payload.</param>
    /// <param name="tenantSlug">The Tenant Slug.</param>
    /// <param name="assetType">The type of asset (e.g. 'logo', 'logo-dark', 'favicon').</param>
    /// <returns>The generated Object Name (Key) that can be used to retrieve the file later.</returns>
    public async Task<string> UploadAssetAsync(IFormFile file, string tenantSlug, string assetType)
    {
        await EnsureBucketExistsAsync();

        // Object name format: {slug}/{assetType}
        var objectName = $"{tenantSlug}/{assetType}";

        using var stream = file.OpenReadStream();
        await minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return objectName;
    }

    public async Task<(Stream Content, string ContentType)> DownloadAssetAsync(string objectName)
    {
        var stat = await minioClient.StatObjectAsync(new StatObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName));

        var stream = new MemoryStream();

        await minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (objectStream, cancellationToken) =>
            {
                await objectStream.CopyToAsync(stream, cancellationToken);
            }));

        stream.Position = 0;
        return (stream, stat.ContentType);
    }

    /// <summary>
    /// Permanently deletes an asset from MinIO storage.
    /// </summary>
    /// <param name="objectName">The exact Key/ObjectName of the file in the bucket.</param>
    public async Task DeleteAssetAsync(string objectName)
    {
        await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName));
    }
}
