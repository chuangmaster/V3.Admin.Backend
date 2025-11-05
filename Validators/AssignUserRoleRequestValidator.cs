using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 為用戶指派角色請求驗證器
/// </summary>
public class AssignUserRoleRequestValidator : AbstractValidator<AssignUserRoleRequest>
{
    /// <summary>
    /// 初始化驗證器
    /// </summary>
    public AssignUserRoleRequestValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotEmpty()
            .WithMessage("角色 ID 列表不能為空")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("所有角色 ID 都必須是有效的 GUID");
    }
}
