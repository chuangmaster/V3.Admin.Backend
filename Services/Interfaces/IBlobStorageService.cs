namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// Azure Blob Storage 服務介面
/// </summary>
/// <remarks>
/// 封裝 Blob 上傳與 SAS Token 產生等操作,以便於單元測試與替換實作
/// </remarks>
public interface IBlobStorageService
{
    /// <summary>
    /// 上傳檔案至 Blob Storage
    /// </summary>
    /// <param name="blobPath">Blob 路徑 (含資料夾與檔名)</param>
    /// <param name="content">檔案內容串流</param>
    /// <param name="contentType">MIME 類型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上傳後的 Blob 路徑</returns>
    Task<string> UploadAsync(
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 產生具時效性的 SAS 下載連結
    /// </summary>
    /// <param name="blobPath">Blob 路徑</param>
    /// <param name="expiresIn">有效期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SAS 下載連結</returns>
    Task<Uri> GenerateReadSasUriAsync(
        string blobPath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    );
}
