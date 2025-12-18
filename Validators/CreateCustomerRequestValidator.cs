using System.Text.RegularExpressions;
using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 新增客戶請求驗證器
/// </summary>
/// <remarks>
/// 驗證新增客戶請求的輸入格式
/// </remarks>
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    private static readonly Regex TaiwanPhoneRegex = new("^09\\d{2}-?\\d{6}$");
    private static readonly Regex TaiwanIdRegex = new("^[A-Z][0-9]{9}$");
    private static readonly Regex ForeignerIdRegex = new("^\\d{8}[A-Z]{2}$");

    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("電話不可為空")
            .Must(phone => TaiwanPhoneRegex.IsMatch(phone))
            .WithMessage("電話格式不正確,請使用 09XXXXXXXX 或 09XX-XXXXXX");

        When(
            x => !string.IsNullOrWhiteSpace(x.Email),
            () =>
            {
                RuleFor(x => x.Email)
                    .MaximumLength(100).WithMessage("Email 長度不可超過 100 字元")
                    .EmailAddress().WithMessage("Email 格式不正確");
            }
        );

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("身分證字號不可為空")
            .Must(idNumber => IsValidIdNumber(idNumber))
            .WithMessage(
                "身分證字號格式不正確,台灣人士請填 1 英文字母 + 9 數字；外籍人士請填 8 位生日 + 2 位大寫英文"
            );
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
