using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 身分證 OCR 辨識請求驗證器
/// </summary>
/// <remarks>
/// 驗證圖片 Base64 與 MIME 類型,並限制檔案大小
/// </remarks>
public class OcrIdCardRequestValidator : AbstractValidator<OcrIdCardRequest>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
    ];

    private const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public OcrIdCardRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("圖片類型不可為空")
            .Must(type => AllowedContentTypes.Contains(type))
            .WithMessage("僅允許上傳 JPG/PNG 圖片");

        RuleFor(x => x.ImageBase64)
            .NotEmpty().WithMessage("圖片內容不可為空")
            .Must(imageBase64 => TryDecodeBase64WithinLimit(imageBase64, MaxBytes))
            .WithMessage("圖片 Base64 格式不正確或檔案大小超過 10MB");

        When(
            x => !string.IsNullOrWhiteSpace(x.FileName),
            () =>
            {
                RuleFor(x => x.FileName)
                    .MaximumLength(255)
                    .WithMessage("檔名長度不可超過 255 字元");
            }
        );
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
