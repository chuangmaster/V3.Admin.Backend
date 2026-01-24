namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 用戶角色 DTO
/// 用於 API 回應
/// </summary>
public class UserRoleDto
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
    /// 角色名稱
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// 指派時間（UTC）
    /// </summary>
    public DateTimeOffset AssignedAt { get; set; }
}
