using System.Text.Json.Serialization;
using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限列表回應
/// </summary>
public class PermissionListResponse : ApiResponseModel
{
    /// <summary>
    /// 隱藏繼承的 Data 屬性，分頁列表使用 Items 代替
    /// </summary>
    [JsonIgnore]
    public override object? Data { get; set; }

    /// <summary>
    /// 權限列表
    /// </summary>
    public List<PermissionDto>? Items { get; set; }

    /// <summary>
    /// 總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 目前頁碼
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }
}
