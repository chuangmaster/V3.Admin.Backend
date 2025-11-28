using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 角色詳細資訊回應資料
/// </summary>
public class RoleDetailResponse
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
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// 版本號（用於樂觀並發控制）
	/// </summary>
	public int Version { get; set; }

	/// <summary>
	/// 該角色擁有的權限列表（對外的 response DTO）
	/// </summary>
	public List<PermissionResponse> Permissions { get; set; } = new();
}
