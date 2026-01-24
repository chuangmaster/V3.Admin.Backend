namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 角色詳細資訊 DTO
/// </summary>
/// <remarks>
/// 包含角色基本資訊和相關的權限列表
/// </remarks>
public class RoleDetailDto
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
    /// 版本號
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 該角色擁有的權限列表
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();
}
