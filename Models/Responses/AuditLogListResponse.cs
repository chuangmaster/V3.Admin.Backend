using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 稽核日誌列表響應
/// </summary>
/// <remarks>
/// 包含分頁稽核日誌資訊和分頁元資料
/// </remarks>
public class AuditLogListResponse : ApiResponseModel
{
    /// <summary>
    /// 稽核日誌列表
    /// </summary>
    public List<AuditLogDto> Items { get; set; } = new();

    /// <summary>
    /// 總記錄筆數
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 當前頁碼
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
