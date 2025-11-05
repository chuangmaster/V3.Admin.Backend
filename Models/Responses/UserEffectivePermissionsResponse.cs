using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶有效權限回應模型
/// </summary>
public class UserEffectivePermissionsResponse : ApiResponseModel<UserEffectivePermissionsDto>
{
    /// <summary>
    /// 初始化 UserEffectivePermissionsResponse
    /// </summary>
    public UserEffectivePermissionsResponse() { }

    /// <summary>
    /// 初始化 UserEffectivePermissionsResponse
    /// </summary>
    public UserEffectivePermissionsResponse(
        UserEffectivePermissionsDto data,
        string message = "查詢成功",
        string code = ResponseCodes.SUCCESS
    )
    {
        Data = data;
        Message = message;
        Code = code;
    }
}
