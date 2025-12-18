using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 建立線下收購單請求驗證器
/// </summary>
/// <remarks>
/// 驗證收購單建立所需欄位,包含客戶選擇、新增商品項目與身分證明圖片
/// </remarks>
public class CreateBuybackOrderRequestValidator : AbstractValidator<CreateBuybackOrderRequest>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
    ];

    private const int MaxIdCardBytes = 10 * 1024 * 1024;

    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public CreateBuybackOrderRequestValidator()
    {
        RuleFor(x => x.OrderType)
            .NotEmpty().WithMessage("服務單類型不可為空")
            .Equal("BUYBACK").WithMessage("US1 僅支援建立收購單 (BUYBACK)");

        RuleFor(x => x.OrderSource)
            .NotEmpty().WithMessage("服務單來源不可為空")
            .Equal("OFFLINE").WithMessage("US1 僅支援線下來源 (OFFLINE)");

        RuleFor(x => x)
            .Must(HasExactlyOneCustomerSelection)
            .WithMessage("請選擇既有客戶或填寫新增客戶資料 (二擇一)");

        When(
            x => x.CustomerId.HasValue,
            () =>
            {
                RuleFor(x => x.CustomerId)
                    .Must(id => id.HasValue && id.Value != Guid.Empty)
                    .WithMessage("CustomerId 不可為空");
            }
        );

        When(
            x => x.NewCustomer is not null,
            () =>
            {
                RuleFor(x => x.NewCustomer!).SetValidator(new CreateCustomerRequestValidator());
            }
        );

        RuleFor(x => x.ProductItems)
            .NotNull().WithMessage("商品項目不可為空")
            .Must(items => items.Count >= 1 && items.Count <= 4)
            .WithMessage("商品項目必須為 1-4 件")
            .Must(HasValidSequenceNumbers)
            .WithMessage("商品序號必須為 1-4 且不可重複");

        RuleForEach(x => x.ProductItems)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.SequenceNumber)
                    .InclusiveBetween(1, 4)
                    .WithMessage("商品序號必須介於 1-4");

                item.RuleFor(x => x.BrandName)
                    .NotEmpty().WithMessage("品牌名稱不可為空")
                    .MaximumLength(100).WithMessage("品牌名稱長度不可超過 100 字元");

                item.RuleFor(x => x.StyleName)
                    .NotEmpty().WithMessage("款式不可為空")
                    .MaximumLength(100).WithMessage("款式長度不可超過 100 字元");

                item.When(
                    x => !string.IsNullOrWhiteSpace(x.InternalCode),
                    () =>
                    {
                        item.RuleFor(x => x.InternalCode)
                            .MaximumLength(50)
                            .WithMessage("內碼長度不可超過 50 字元");
                    }
                );
            });

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0)
            .WithMessage("總金額必須大於 0");

        RuleFor(x => x.IdCardImageContentType)
            .NotEmpty().WithMessage("身分證明圖片類型不可為空")
            .Must(type => AllowedContentTypes.Contains(type))
            .WithMessage("身分證明僅允許 JPG/PNG 圖片");

        RuleFor(x => x.IdCardImageFileName)
            .NotEmpty().WithMessage("身分證明檔名不可為空")
            .MaximumLength(255)
            .WithMessage("身分證明檔名長度不可超過 255 字元");

        RuleFor(x => x.IdCardImageBase64)
            .NotEmpty().WithMessage("身分證明圖片不可為空")
            .Must(base64 => TryDecodeBase64WithinLimit(base64, MaxIdCardBytes))
            .WithMessage("身分證明圖片 Base64 格式不正確或檔案大小超過 10MB");
    }

    /// <summary>
    /// 驗證客戶選擇必須剛好二擇一
    /// </summary>
    private static bool HasExactlyOneCustomerSelection(CreateBuybackOrderRequest request)
    {
        bool hasCustomerId = request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty;
        bool hasNewCustomer = request.NewCustomer is not null;
        return hasCustomerId ^ hasNewCustomer;
    }

    /// <summary>
    /// 驗證商品序號必須為 1-4 且不可重複
    /// </summary>
    private static bool HasValidSequenceNumbers(List<CreateBuybackProductItemRequest> items)
    {
        if (items.Count == 0)
        {
            return false;
        }

        var distinct = items.Select(x => x.SequenceNumber).Distinct().ToList();
        if (distinct.Count != items.Count)
        {
            return false;
        }

        return distinct.All(x => x is >= 1 and <= 4);
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
