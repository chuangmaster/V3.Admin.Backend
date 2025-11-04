namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 權限實體
/// </summary>
/// <remarks>
/// 對應資料庫 permissions 資料表，定義系統中的訪問或操作授權
/// 支援兩種類型：路由權限（控制頁面訪問）和功能權限（控制操作權限）
/// </remarks>
public class Permission
{
    /// <summary>
    /// 權限唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 權限代碼（唯一）
    /// </summary>
    /// <remarks>
    /// 功能權限格式: resource.action（如 inventory.create, users.delete）
    /// 路由權限格式: 自訂字串（如 dashboard_access, inventory_page_access）
    /// </remarks>
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
    /// 權限類型（route: 路由權限, function: 功能權限）
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// 路由路徑（僅路由權限使用）
    /// </summary>
    /// <remarks>
    /// 如 /inventory, /users/profile
    /// </remarks>
    public string? RoutePath { get; set; }

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
