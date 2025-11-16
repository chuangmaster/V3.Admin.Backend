using FluentValidation;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// PermissionCode 格式驗證器
/// </summary>
/// <remarks>
/// 驗證權限代碼是否符合統一編碼規範：
/// - 格式: resource.action 或 resource.subresource.action
/// - 長度: 3-100 字元
/// - 字元: 字母、數字、點號、下劃線
/// - 開頭和結尾不能是點號或下劃線
/// </remarks>
public class PermissionCodeValidator : AbstractValidator<string>
{
    public PermissionCodeValidator()
    {
        RuleFor(code => code)
            .NotEmpty().WithMessage("權限代碼不可為空")
            .Length(3, 100).WithMessage("權限代碼長度須為 3-100 字元")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$")
            .WithMessage("權限代碼格式不正確，只允許字母、數字、點號、下劃線，且開頭和結尾不能是點號或下劃線");
    }

    /// <summary>
    /// 驗證權限代碼格式是否正確（靜態方法）
    /// </summary>
    /// <param name="permissionCode">權限代碼</param>
    /// <returns>是否有效</returns>
    public static bool IsValid(string permissionCode)
    {
        var validator = new PermissionCodeValidator();
        var result = validator.Validate(permissionCode);
        return result.IsValid;
    }

    /// <summary>
    /// 取得驗證錯誤訊息
    /// </summary>
    /// <param name="permissionCode">權限代碼</param>
    /// <returns>錯誤訊息列表</returns>
    public static List<string> GetErrors(string permissionCode)
    {
        var validator = new PermissionCodeValidator();
        var result = validator.Validate(permissionCode);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }
}
