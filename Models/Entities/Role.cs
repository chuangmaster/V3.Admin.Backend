namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 角色實體
/// </summary>
/// <remarks>
/// 對應資料庫 roles 資料表，代表權限的集合
/// </remarks>
public class Role
{
    /// <summary>
    /// 角色唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色名稱（唯一）
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 建立者 ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 最後更新者 ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 是否已刪除（軟刪除標記）
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 刪除時間 (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除操作者 ID
    /// </summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// 版本號（樂觀並發控制）
    /// </summary>
    public int Version { get; set; }
}
