using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 用戶有效權限 DTO
/// 用於 API 回應，包含用戶的所有合併權限
/// </summary>
public class UserEffectivePermissionsDto
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用戶擁有的所有權限列表（聯集）
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = [];
}
