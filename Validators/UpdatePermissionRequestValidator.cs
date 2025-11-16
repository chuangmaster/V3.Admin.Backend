using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 更新權限請求驗證器
/// </summary>
public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .MaximumLength(200).WithMessage("權限名稱最多 200 字元");

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(1).WithMessage("版本號必須大於等於 1");
    }
}
