using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 重設密碼請求驗證器
/// </summary>
/// <remarks>
/// 驗證管理員重設用戶密碼請求的輸入格式
/// </remarks>
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密碼為必填")
            .MinimumLength(8).WithMessage("密碼長度至少 8 個字元")
            .MaximumLength(100).WithMessage("密碼長度最多 100 個字元")
            .Matches(@"[A-Z]").WithMessage("密碼必須包含至少一個大寫字母")
            .Matches(@"[a-z]").WithMessage("密碼必須包含至少一個小寫字母")
            .Matches(@"[0-9]").WithMessage("密碼必須包含至少一個數字");

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(0).WithMessage("版本號必須大於或等於 0");
    }
}
