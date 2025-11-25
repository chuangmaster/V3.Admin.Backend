using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限回應
/// </summary>
public class PermissionResponse
{
    /// <summary>
    /// API 回應碼（此 DTO 同時支援被序列化成 API wrapper 的情況）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 當此類被當作 API wrapper 反序列化時，Data 會包含實際的 PermissionResponse 資料
    /// </summary>
    public PermissionResponse? Data { get; set; }

    /// <summary>
    /// 權限唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 權限代碼
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 權限名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 權限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 權限類型
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 版本號
    /// </summary>
    public int Version { get; set; }
}
