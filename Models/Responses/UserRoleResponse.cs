using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶角色回應模型
/// 包含用戶的所有角色列表
/// </summary>
public class UserRoleResponse : ApiResponseModel<List<UserRoleDto>>
{
    /// <summary>
    /// 初始化 UserRoleResponse
    /// </summary>
    public UserRoleResponse() { }

    /// <summary>
    /// 初始化 UserRoleResponse
    /// </summary>
    public UserRoleResponse(
        List<UserRoleDto> data,
        string message = "查詢成功",
        string code = ResponseCodes.SUCCESS
    )
    {
        Data = data;
        Message = message;
        Code = code;
    }
}
