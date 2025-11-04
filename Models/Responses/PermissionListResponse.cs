using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 權限列表回應
/// </summary>
public class PermissionListResponse : ApiResponseModel
{
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
