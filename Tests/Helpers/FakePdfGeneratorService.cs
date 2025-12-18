using System.Text;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Tests.Helpers;

/// <summary>
/// 測試用 PDF 產生/簽章合併服務
/// </summary>
/// <remarks>
/// 整合測試環境中避免依賴 PDFsharp、字型或作業系統圖形子系統。
/// 只要能回傳可 Base64 編碼的位元組即可滿足目前 US1 API 流程測試。
/// </remarks>
public class FakePdfGeneratorService : IPdfGeneratorService
{
    /// <summary>
    /// 產生預覽 PDF 位元組
    /// </summary>
    public Task<byte[]> GeneratePreviewAsync(
        string templateName,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("templateName 不可為空", nameof(templateName));
        }

        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 最小化的「看起來像 PDF」內容即可，避免任何外部相依。
        string content = $"%PDF-1.4\n% Fake PDF for integration tests\nTemplate: {templateName}\nFieldCount: {fields.Count}\n";
        return Task.FromResult(Encoding.UTF8.GetBytes(content));
    }

    /// <summary>
    /// 合併簽章至 PDF (測試用：不修改 PDF 內容)
    /// </summary>
    public Task<byte[]> MergeSignatureAsync(
        byte[] pdfBytes,
        string signatureBase64Png,
        int pageIndex,
        double x,
        double y,
        double width,
        double height,
        CancellationToken cancellationToken = default
    )
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("pdfBytes 不可為空", nameof(pdfBytes));
        }

        if (string.IsNullOrWhiteSpace(signatureBase64Png))
        {
            throw new ArgumentException("signatureBase64Png 不可為空", nameof(signatureBase64Png));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 仍然檢查 Base64 是否可解碼，以模擬基本輸入驗證。
        _ = Convert.FromBase64String(signatureBase64Png);

        return Task.FromResult(pdfBytes);
    }
}
