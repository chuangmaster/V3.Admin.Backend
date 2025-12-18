using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 合併簽章請求驗證器
/// </summary>
/// <remarks>
/// 驗證 PDF 與簽章 Base64、座標與尺寸參數
/// </remarks>
public class MergeSignatureRequestValidator : AbstractValidator<MergeSignatureRequest>
{
    private const int MaxPdfBytes = 20 * 1024 * 1024;
    private const int MaxSignatureBytes = 2 * 1024 * 1024;

    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public MergeSignatureRequestValidator()
    {
        RuleFor(x => x.PdfBase64)
            .NotEmpty().WithMessage("PDF 內容不可為空")
            .Must(pdfBase64 => TryDecodeBase64WithinLimit(pdfBase64, MaxPdfBytes))
            .WithMessage("PDF Base64 格式不正確或檔案大小超過限制");

        RuleFor(x => x.SignatureBase64Png)
            .NotEmpty().WithMessage("簽章圖片不可為空")
            .Must(sigBase64 => TryDecodeBase64WithinLimit(sigBase64, MaxSignatureBytes))
            .WithMessage("簽章 Base64 格式不正確或檔案大小超過限制");

        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("PageIndex 不可小於 0");

        RuleFor(x => x.X)
            .GreaterThanOrEqualTo(0)
            .WithMessage("X 座標不可小於 0")
            .Must(value => !double.IsNaN(value) && !double.IsInfinity(value))
            .WithMessage("X 座標數值不正確");

        RuleFor(x => x.Y)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Y 座標不可小於 0")
            .Must(value => !double.IsNaN(value) && !double.IsInfinity(value))
            .WithMessage("Y 座標數值不正確");

        RuleFor(x => x.Width)
            .GreaterThan(0)
            .WithMessage("簽章寬度必須大於 0")
            .Must(value => !double.IsNaN(value) && !double.IsInfinity(value))
            .WithMessage("簽章寬度數值不正確");

        RuleFor(x => x.Height)
            .GreaterThan(0)
            .WithMessage("簽章高度必須大於 0")
            .Must(value => !double.IsNaN(value) && !double.IsInfinity(value))
            .WithMessage("簽章高度數值不正確");
    }

    /// <summary>
    /// 嘗試解碼 Base64,並限制最大位元組大小
    /// </summary>
    private static bool TryDecodeBase64WithinLimit(string base64, int maxBytes)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return false;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            return bytes.Length <= maxBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
