using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 變更密碼請求驗證器
/// </summary>
/// <remarks>
/// 驗證變更密碼請求的輸入格式
/// </remarks>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("舊密碼不可為空")
            .MinimumLength(8).WithMessage("舊密碼長度至少 8 字元");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密碼不可為空")
            .MinimumLength(8).WithMessage("新密碼至少 8 個字元");

        RuleFor(x => x)
            .Must(x => x.NewPassword != x.OldPassword)
            .WithMessage("新密碼不可與舊密碼相同")
            .WithName("NewPassword");
    }
}
