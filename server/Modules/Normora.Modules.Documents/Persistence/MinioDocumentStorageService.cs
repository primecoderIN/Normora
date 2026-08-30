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
public class MinioDocumentStorageService : IDocumentStorageService
{
    private readonly IMinioClient _minioClient;
    private const string BucketName = "normora-documents";

    public MinioDocumentStorageService(IMinioClient minioClient)
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
    /// <param name="tenantId">The Tenant ID, used to prefix the object name for logical partitioning inside the bucket.</param>
    /// <returns>The generated Object Name (Key) that can be used to retrieve the file later.</returns>
    public async Task<string> UploadDocumentAsync(IFormFile file, string tenantId)
    {
        // Prefixing with tenantId creates pseudo-folders in the S3 bucket for organization.
        var objectName = $"{tenantId}/{Guid.NewGuid()}_{file.FileName}";

        using var stream = file.OpenReadStream();
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        return objectName;
    }

    /// <summary>
    /// Permanently deletes a file from MinIO storage.
    /// </summary>
    /// <param name="objectName">The exact Key/ObjectName of the file in the bucket.</param>
    public async Task DeleteDocumentAsync(string objectName)
    {
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
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
        return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithExpiry(60 * 60)); // The URL becomes invalid after 1 hour for security.
    }
}
