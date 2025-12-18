using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// Azure Blob Storage 服務
/// </summary>
/// <remarks>
/// 負責附件上傳與產生具時效性的 SAS 下載連結,並以 MemoryCache 減少重複產生 SAS 的成本
/// </remarks>
public class BlobStorageService : IBlobStorageService
{
    private readonly AzureBlobStorageSettings _settings;
    private readonly IMemoryCache _memoryCache;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(IOptions<AzureBlobStorageSettings> options, IMemoryCache memoryCache)
    {
        _settings = options.Value;
        _memoryCache = memoryCache;

        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new InvalidOperationException("AZURE_BLOB_CONNECTION_STRING 未設定");
        }

        if (string.IsNullOrWhiteSpace(_settings.ContainerName))
        {
            throw new InvalidOperationException("AZURE_BLOB_CONTAINER_NAME 未設定");
        }

        _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
    }

    /// <summary>
    /// 上傳檔案至 Blob Storage
    /// </summary>
    public async Task<string> UploadAsync(
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("blobPath 不可為空", nameof(blobPath));
        }

        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
        await containerClient.CreateIfNotExistsAsync(
            publicAccessType: PublicAccessType.None,
            cancellationToken: cancellationToken
        );

        BlobClient blobClient = containerClient.GetBlobClient(blobPath);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };

        await blobClient.UploadAsync(content, options, cancellationToken);

        _memoryCache.Remove(GetSasCacheKey(blobPath));

        return blobPath;
    }

    /// <summary>
    /// 產生具時效性的 SAS 下載連結
    /// </summary>
    public async Task<Uri> GenerateReadSasUriAsync(
        string blobPath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("blobPath 不可為空", nameof(blobPath));
        }

        if (expiresIn <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresIn), "expiresIn 必須大於 0");
        }

        var cacheKey = GetSasCacheKey(blobPath);
        if (_memoryCache.TryGetValue(cacheKey, out Uri? cachedUri) && cachedUri is not null)
        {
            return cachedUri;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException($"找不到 Blob: {blobPath}", blobPath);
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _settings.ContainerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

        var ttl = expiresIn - TimeSpan.FromMinutes(5);
        if (ttl < TimeSpan.FromMinutes(1))
        {
            ttl = TimeSpan.FromMinutes(1);
        }

        _memoryCache.Set(cacheKey, sasUri, ttl);

        return sasUri;
    }

    private static string GetSasCacheKey(string blobPath)
    {
        return $"sas:{blobPath}";
    }
}
