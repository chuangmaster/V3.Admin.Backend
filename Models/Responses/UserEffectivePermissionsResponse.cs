using System;
using System.Collections.Generic;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶有效權限 API Response DTO
/// </summary>
public class UserEffectivePermissionsResponse
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用戶擁有的所有權限列表（聯集）
    /// </summary>
    public List<PermissionResponseDto> Permissions { get; set; } = new();
}
