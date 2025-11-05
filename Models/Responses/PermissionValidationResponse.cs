namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限驗證結果回應模型
/// </summary>
public class PermissionValidationResponse : ApiResponseModel<bool>
{
    /// <summary>
    /// 用戶是否擁有該權限
    /// </summary>
    public bool HasPermission
    {
        get => Data;
        set => Data = value;
    }

    /// <summary>
    /// 初始化 PermissionValidationResponse
    /// </summary>
    public PermissionValidationResponse() { }

    /// <summary>
    /// 初始化 PermissionValidationResponse
    /// </summary>
    public PermissionValidationResponse(
        bool hasPermission,
        string message = "驗證完成",
        string code = ResponseCodes.SUCCESS
    )
    {
        Data = hasPermission;
        Message = message;
        Code = code;
    }
}
