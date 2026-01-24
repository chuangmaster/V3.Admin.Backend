namespace V3.Admin.Backend.Models.Entities;

/// <summary>
/// 用戶角色關聯實體
/// 表示用戶被指派的角色
/// </summary>
public class UserRole
{
    /// <summary>
    /// 用戶角色指派記錄 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 指派者用戶 ID（操作員）
    /// </summary>
    public Guid AssignedBy { get; set; }

    /// <summary>
    /// 指派時間（UTC）
    /// </summary>
    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>
    /// 軟刪除標記
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 刪除時間（UTC）
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// 刪除者用戶 ID
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
