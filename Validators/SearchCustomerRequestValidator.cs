using System.Text.RegularExpressions;
using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 查詢客戶請求驗證器
/// </summary>
/// <remarks>
/// 驗證客戶查詢條件,至少需提供一個條件
/// </remarks>
public class SearchCustomerRequestValidator : AbstractValidator<SearchCustomerRequest>
{
    private static readonly Regex TaiwanPhoneRegex = new("^09\\d{2}-?\\d{6}$");
    private static readonly Regex TaiwanIdRegex = new("^[A-Z][0-9]{9}$");
    private static readonly Regex ForeignerIdRegex = new("^\\d{8}[A-Z]{2}$");

    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public SearchCustomerRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAnyCriteria)
            .WithMessage("請至少提供一個查詢條件");

        When(
            x => !string.IsNullOrWhiteSpace(x.Name),
            () =>
            {
                RuleFor(x => x.Name)
                    .MaximumLength(100)
                    .WithMessage("姓名查詢條件長度不可超過 100 字元");
            }
        );

        When(
            x => !string.IsNullOrWhiteSpace(x.Email),
            () =>
            {
                RuleFor(x => x.Email)
                    .MaximumLength(100)
                    .WithMessage("Email 查詢條件長度不可超過 100 字元");
            }
        );

        When(
            x => !string.IsNullOrWhiteSpace(x.PhoneNumber),
            () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Must(phone => TaiwanPhoneRegex.IsMatch(phone!))
                    .WithMessage("電話格式不正確,請使用 09XXXXXXXX 或 09XX-XXXXXX");
            }
        );

        When(
            x => !string.IsNullOrWhiteSpace(x.IdNumber),
            () =>
            {
                RuleFor(x => x.IdNumber)
                    .Must(idNumber => IsValidIdNumber(idNumber!))
                    .WithMessage(
                        "身分證字號格式不正確,台灣人士請填 1 英文字母 + 9 數字；外籍人士請填 8 位生日 + 2 位大寫英文"
                    );
            }
        );
    }

    /// <summary>
    /// 檢查是否至少提供一個查詢條件
    /// </summary>
    private static bool HasAnyCriteria(SearchCustomerRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Name)
            || !string.IsNullOrWhiteSpace(request.PhoneNumber)
            || !string.IsNullOrWhiteSpace(request.Email)
            || !string.IsNullOrWhiteSpace(request.IdNumber);
    }

    /// <summary>
    /// 驗證身分證字號格式 (台灣/外籍)
    /// </summary>
    private static bool IsValidIdNumber(string idNumber)
    {
        string normalized = idNumber.Trim().ToUpperInvariant();
        return TaiwanIdRegex.IsMatch(normalized) || ForeignerIdRegex.IsMatch(normalized);
    }
}
