namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 帳號列表 DTO (Service 層使用)
/// </summary>
/// <remarks>
/// 用於 Service 層傳遞帳號列表資訊
/// </remarks>
public class AccountListDto
{
    /// <summary>
    /// 帳號清單
    /// </summary>
    public List<AccountDto> Items { get; set; } = new();

    /// <summary>
    /// 總數量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 當前頁碼
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁數量
    /// </summary>
    public int PageSize { get; set; }
}
