namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限 API Response DTO
/// </summary>
/// <remarks>
/// 用於 API 回應的權限資訊，從 Service 層 PermissionDto 轉換而來
/// </remarks>
public class PermissionResponseDto
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
}
