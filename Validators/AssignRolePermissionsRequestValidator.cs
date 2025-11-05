using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 分配角色權限請求驗證器
/// </summary>
public class AssignRolePermissionsRequestValidator : AbstractValidator<AssignRolePermissionsRequest>
{
    /// <summary>
    /// 初始化驗證規則
    /// </summary>
    public AssignRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotEmpty()
            .WithMessage("權限 ID 陣列不可為空");

        RuleForEach(x => x.PermissionIds)
            .NotEmpty()
            .WithMessage("權限 ID 不可為空");
    }
}
