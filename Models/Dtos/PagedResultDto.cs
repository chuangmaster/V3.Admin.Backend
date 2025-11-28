namespace V3.Admin.Backend.Models.Dtos;

/// <summary>
/// 提供分頁結果的通用數據傳輸對象。
/// </summary>
/// <typeparam name="T">項目的類型。</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// 當前頁的項目集合。
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// 所有頁面的項目總數。
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 當前頁碼。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每頁的項目數。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 支援解構語法 (e.g. var (items, total) = pagedResult)
    /// </summary>
    public void Deconstruct(out List<T> items, out long totalCount)
    {
        items = Items?.ToList() ?? new List<T>();
        totalCount = TotalCount;
    }
}