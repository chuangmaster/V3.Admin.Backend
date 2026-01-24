namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 角色 DTO
/// </summary>
/// <remarks>
/// 用於 API 回應，包含角色基本資訊
/// </remarks>
public class RoleDto
{
    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 版本號（用於樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
