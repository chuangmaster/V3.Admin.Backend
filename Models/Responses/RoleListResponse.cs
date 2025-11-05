using System.Text.Json.Serialization;
using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 角色列表回應
/// </summary>
public class RoleListResponse : ApiResponseModel
{
    /// <summary>
    /// 隱藏繼承的 Data 屬性，分頁列表使用 Items 代替
    /// </summary>
    [JsonIgnore]
    public override object? Data { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<RoleDto> Items { get; set; } = new();

    /// <summary>
    /// 總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 頁碼
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }
}
