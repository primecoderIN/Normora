using Microsoft.AspNetCore.Http;
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
public class MinioBrandingStorageService : IBrandingStorageService
{
    private readonly IMinioClient _minioClient;
    private const string BucketName = "normora-branding";

    public MinioBrandingStorageService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if the configured bucket exists in MinIO during startup, and creates it if it doesn't.
    /// </summary>
    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
                
                // For branding assets, we could set the bucket policy to public-read, 
                // but since we are proxying via the API to avoid internal/external DNS issues,
                // we leave the bucket private and let the API read from it using credentials.
            }
        }
        catch (MinioException e)
        {
            Console.WriteLine($"Error occurred: {e}");
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
        // Object name format: {slug}/{assetType}
        var objectName = $"{tenantSlug}/{assetType}";

        using var stream = file.OpenReadStream();
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return objectName;
    }

    public async Task<(Stream Content, string ContentType)> DownloadAssetAsync(string objectName)
    {
        var stat = await _minioClient.StatObjectAsync(new StatObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName));

        var stream = new MemoryStream();

        await _minioClient.GetObjectAsync(new GetObjectArgs()
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
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName));
    }
}
