using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 更新帳號請求驗證器
/// </summary>
/// <remarks>
/// 驗證更新帳號請求的輸入格式
/// </remarks>
public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");
    }
}
