namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// PDF 產生與簽章合併服務介面
/// </summary>
/// <remarks>
/// 使用 PDFsharp 填充 PDF 模板、合併 Base64 PNG 簽章,並輸出可預覽/儲存的 PDF 位元組
/// </remarks>
public interface IPdfGeneratorService
{
    /// <summary>
    /// 依欄位值填充 PDF 模板並輸出預覽 PDF
    /// </summary>
    /// <param name="templateName">模板名稱 (由實作決定對應檔案)</param>
    /// <param name="fields">要填入的欄位與值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>預覽 PDF 位元組</returns>
    Task<byte[]> GeneratePreviewAsync(
        string templateName,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 將 Base64 PNG 簽章合併到 PDF 並輸出合併後 PDF
    /// </summary>
    /// <param name="pdfBytes">原始 PDF 位元組</param>
    /// <param name="signatureBase64Png">Base64 PNG (不含 data URI 前綴)</param>
    /// <param name="pageIndex">頁碼 (0-based)</param>
    /// <param name="x">X 座標 (點)
    /// </param>
    /// <param name="y">Y 座標 (點)
    /// </param>
    /// <param name="width">寬度 (點)</param>
    /// <param name="height">高度 (點)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合併後 PDF 位元組</returns>
    Task<byte[]> MergeSignatureAsync(
        byte[] pdfBytes,
        string signatureBase64Png,
        int pageIndex,
        double x,
        double y,
        double width,
        double height,
        CancellationToken cancellationToken = default
    );
}
