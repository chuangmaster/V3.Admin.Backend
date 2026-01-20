using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 登入請求驗證器
/// </summary>
/// <remarks>
/// 驗證登入請求的輸入格式
/// </remarks>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(8).WithMessage("密碼至少 8 個字元");
    }
}
