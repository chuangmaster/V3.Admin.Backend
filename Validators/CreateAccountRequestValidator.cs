using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 新增帳號請求驗證器
/// </summary>
/// <remarks>
/// 驗證新增帳號請求的輸入格式
/// </remarks>
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("帳號僅允許英數字與底線");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(8).WithMessage("密碼長度至少 8 字元");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");
    }
}
