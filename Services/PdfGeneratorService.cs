using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// PDF 產生與簽章合併服務
/// </summary>
/// <remarks>
/// 使用 PDFsharp 生成可預覽的 PDF,並支援將 Base64 PNG 簽章合併至指定位置
/// </remarks>
public class PdfGeneratorService : IPdfGeneratorService
{
    /// <summary>
    /// 依欄位值產生預覽 PDF
    /// </summary>
    /// <remarks>
    /// 目前以簡化版示範：直接建立一頁 PDF 並以文字列出欄位值
    /// 後續可替換為「載入模板 PDF + 定位填值」的正式流程
    /// </remarks>
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

        using var document = new PdfDocument();
        PdfPage page = document.AddPage();

        using XGraphics gfx = XGraphics.FromPdfPage(page);
        XFont font = new XFont("Microsoft JhengHei", 12);

        gfx.DrawString($"Template: {templateName}", font, XBrushes.Black, new XPoint(40, 40));

        double y = 70;
        foreach ((string key, string value) in fields)
        {
            gfx.DrawString($"{key}: {value}", font, XBrushes.Black, new XPoint(40, y));
            y += 20;
        }

        using var ms = new MemoryStream();
        document.Save(ms);

        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// 將 Base64 PNG 簽章合併到 PDF
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

        if (pageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "width/height 必須大於 0");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var input = new MemoryStream(pdfBytes);
        using PdfDocument document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        if (pageIndex >= document.Pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "pageIndex 超出 PDF 頁數範圍");
        }

        PdfPage page = document.Pages[pageIndex];
        using XGraphics gfx = XGraphics.FromPdfPage(page);

        byte[] signatureBytes = Convert.FromBase64String(signatureBase64Png);
        using var signatureStream = new MemoryStream(signatureBytes);
        using XImage signatureImage = XImage.FromStream(signatureStream);

        var rect = new XRect(x, y, width, height);
        gfx.DrawImage(signatureImage, rect);

        using var output = new MemoryStream();
        document.Save(output);

        return Task.FromResult(output.ToArray());
    }
}
