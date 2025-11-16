using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 建立權限請求驗證器
/// </summary>
public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.PermissionCode)
            .NotEmpty().WithMessage("權限代碼不可為空")
            .Length(3, 100).WithMessage("權限代碼長度須為 3-100 字元")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$")
            .WithMessage("權限代碼格式不正確，只允許字母、數字、點號、下劃線");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .MaximumLength(200).WithMessage("權限名稱最多 200 字元");

        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("權限類型不可為空")
            .Must(x => x == "function" || x == "view").WithMessage("權限類型必須是 'function' 或 'view'");
    }
}
