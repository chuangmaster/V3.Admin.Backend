namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 權限 DTO
/// </summary>
/// <remarks>
/// 用於 API 回應，包含權限的基本資訊
/// </remarks>
public class PermissionDto
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
