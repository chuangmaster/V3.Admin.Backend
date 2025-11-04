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
            .MaximumLength(100).WithMessage("權限代碼最多 100 字元")
            .Matches(@"^[a-z0-9._-]+$").WithMessage("權限代碼只能包含小寫字母、數字、點、下劃線和連字號");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .MaximumLength(200).WithMessage("權限名稱最多 200 字元");

        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("權限類型不可為空")
            .Must(x => x == "route" || x == "function").WithMessage("權限類型必須是 'route' 或 'function'");

        RuleFor(x => x.RoutePath)
            .NotEmpty().When(x => x.PermissionType == "route")
            .WithMessage("路由權限必須指定路由路徑")
            .MaximumLength(500).WithMessage("路由路徑最多 500 字元");
    }
}
