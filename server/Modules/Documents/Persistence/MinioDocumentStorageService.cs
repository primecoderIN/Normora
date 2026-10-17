using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// A storage service implementation that interacts with a MinIO S3-compatible object storage server.
/// MinIO is used to physically store the uploaded documents, keeping large binary data out of the PostgreSQL database.
/// </summary>
public class MinioDocumentStorageService(IMinioClient minioClient) : IDocumentStorageService
{
    private const string BucketName = "normora-documents";
    private static volatile bool _bucketEnsured;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Checks if the configured bucket exists in MinIO lazily on first access, and creates it if it doesn't.
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
            Console.WriteLine($"Error occurred: {e}");
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
    /// <param name="tenantId">The Tenant ID, used to prefix the object name for logical partitioning inside the bucket.</param>
    /// <returns>The generated Object Name (Key) that can be used to retrieve the file later.</returns>
    public async Task<string> UploadDocumentAsync(IFormFile file, string tenantId)
    {
        await EnsureBucketExistsAsync();

        // Prefix the file path with the TenantId to physically isolate organizations' documents inside the shared S3 bucket
        var objectName = $"{tenantId}/{Guid.NewGuid()}_{file.FileName}";

        using var stream = file.OpenReadStream();
        await minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return objectName;
    }

    public async Task<Stream> DownloadDocumentAsync(string objectName)
    {
        var stream = new MemoryStream();

        await minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (objectStream, cancellationToken) =>
            {
                await objectStream.CopyToAsync(stream, cancellationToken);
            }));

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Permanently deletes a file from MinIO storage.
    /// </summary>
    /// <param name="objectName">The exact Key/ObjectName of the file in the bucket.</param>
    public async Task DeleteDocumentAsync(string objectName)
    {
        await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName));
    }

    /// <summary>
    /// Generates a temporary, pre-signed URL that allows the frontend to download the file directly from MinIO,
    /// bypassing the .NET API to save bandwidth.
    /// </summary>
    /// <param name="objectName">The exact Key/ObjectName.</param>
    /// <returns>A secure, time-limited URL.</returns>
    public async Task<string> GetDocumentUrlAsync(string objectName)
    {
        // Generate a secure, time-limited download link directly to S3 so large files don't proxy through our API servers
        return await minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithExpiry(60 * 60)); // The URL becomes invalid after 1 hour for security.
    }
}
