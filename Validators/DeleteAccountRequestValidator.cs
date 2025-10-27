using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 刪除帳號請求驗證器
/// </summary>
/// <remarks>
/// 驗證刪除帳號請求的輸入格式
/// </remarks>
public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    /// <summary>
    /// 建構函式,定義驗證規則
    /// </summary>
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Confirmation)
            .NotEmpty().WithMessage("確認訊息不可為空")
            .Equal("CONFIRM").WithMessage("確認訊息必須輸入 'CONFIRM'");
    }
}
