namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 帳號列表回應模型 (分頁)
/// </summary>
/// <remarks>
/// 用於回傳帳號列表給客戶端,包含分頁資訊
/// </remarks>
public class AccountListResponse
{
    /// <summary>
    /// 帳號清單
    /// </summary>
    public List<AccountResponse> Items { get; set; } = new();

    /// <summary>
    /// 總數量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 當前頁碼 (從 1 開始)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁數量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; set; }
}
