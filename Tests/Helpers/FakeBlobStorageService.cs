using System.Collections.Concurrent;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Tests.Helpers;

/// <summary>
/// 測試用 BlobStorageService 替身，避免整合測試依賴 Azure Blob 設定與外部資源。
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

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

        cancellationToken.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        _store[blobPath] = ms.ToArray();

        _ = contentType;

        return blobPath;
    }

    public Task<Uri> GenerateReadSasUriAsync(
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

        cancellationToken.ThrowIfCancellationRequested();

        if (!_store.ContainsKey(blobPath))
        {
            throw new FileNotFoundException($"找不到 Blob: {blobPath}", blobPath);
        }

        var url = $"https://fake-blob.local/{Uri.EscapeDataString(blobPath)}?sas=fake&exp={expiresIn.TotalSeconds:0}";
        return Task.FromResult(new Uri(url));
    }
}
