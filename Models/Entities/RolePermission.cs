namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 角色權限關聯實體
/// </summary>
/// <remarks>
/// 對應資料庫 role_permissions 資料表，連接角色與權限的多對多關係
/// </remarks>
public class RolePermission
{
    /// <summary>
    /// 關聯唯一識別碼
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 權限 ID
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// 分配時間 (UTC)
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// 分配者 ID
    /// </summary>
    public Guid? AssignedBy { get; set; }
}
