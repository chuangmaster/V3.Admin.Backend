using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限回應
/// </summary>
public class PermissionResponse
{

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
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 版本號
    /// </summary>
    public int Version { get; set; }
}
