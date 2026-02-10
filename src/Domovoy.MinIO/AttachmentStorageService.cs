using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Domovoy.Shared.Models;

namespace Domovoy.MinIO;

public class AttachmentStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<AttachmentStorageService> _logger;
    private bool _bucketEnsured;

    public AttachmentStorageService(
        IMinioClient minioClient,
        MinioSettings settings,
        ILogger<AttachmentStorageService> logger)
    {
        _minioClient = minioClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        if (_bucketEnsured) return;

        var exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName),
            cancellationToken);

        if (!exists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName),
                cancellationToken);
            _logger.LogInformation("Created MinIO bucket {BucketName}", _settings.BucketName);
        }

        _bucketEnsured = true;
    }

    public async Task UploadAsync(
        string storageKey,
        Stream stream,
        string contentType,
        long size,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(storageKey)
                .WithStreamData(stream)
                .WithObjectSize(size)
                .WithContentType(contentType),
            cancellationToken);

        _logger.LogInformation("Uploaded {StorageKey} ({Size} bytes) to MinIO", storageKey, size);
    }

    public async Task<Stream> GetDownloadStreamAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(storageKey)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)),
            cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }
}
