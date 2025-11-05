using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 更新角色請求驗證器
/// </summary>
public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    /// <summary>
    /// 初始化驗證規則
    /// </summary>
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("角色名稱不可為空")
            .Length(1, 100)
            .WithMessage("角色名稱必須在 1-100 字元之間");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("角色描述不可超過 500 字元")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(1)
            .WithMessage("版本號必須大於等於 1");
    }
}
