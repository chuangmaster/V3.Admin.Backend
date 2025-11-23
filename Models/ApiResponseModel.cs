using System.Text.Json.Serialization;

namespace V3.Admin.Backend.Models;

/// <summary>
/// 標準 API 響應模型，結合 HTTP 狀態碼與業務邏輯回應碼
/// </summary>
/// <typeparam name="T">響應資料類型</typeparam>
public class ApiResponseModel<T>
{
    /// <summary>
    /// 請求是否成功（通常與 HTTP 狀態碼對應）
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 業務邏輯回應碼，用於細分不同的業務場景
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 響應訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 響應資料
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual T? Data { get; set; }

    /// <summary>
    /// 時間戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 追蹤 ID，用於問題排查
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 建立成功響應
    /// </summary>
    public static ApiResponseModel<T> CreateSuccess(
        T? data = default,
        string message = "操作成功",
        string code = "SUCCESS"
    )
    {
        return new ApiResponseModel<T>
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
        };
    }

    /// <summary>
    /// 建立失敗響應
    /// </summary>
    public static ApiResponseModel<T> CreateFailure(
        string message,
        string code = "FAILURE",
        T? data = default
    )
    {
        return new ApiResponseModel<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = data,
        };
    }
}

/// <summary>
/// 無資料的 API 響應模型
/// </summary>
public class ApiResponseModel : ApiResponseModel<object>
{
    /// <summary>
    /// 建立成功響應（無資料）
    /// </summary>
    public static ApiResponseModel CreateSuccess(
        string message = "操作成功",
        string code = "SUCCESS"
    )
    {
        return new ApiResponseModel
        {
            Success = true,
            Code = code,
            Message = message,
        };
    }

    /// <summary>
    /// 建立失敗響應（無資料）
    /// </summary>
    public static ApiResponseModel CreateFailure(string message, string code = "FAILURE")
    {
        return new ApiResponseModel
        {
            Success = false,
            Code = code,
            Message = message,
        };
    }
}

/// <summary>
/// 專用分頁回應模型（避免 Data.Items 的巢狀），直接把分頁欄位放在回應頂層
/// </summary>
/// <typeparam name="TItem">分頁項目類型</typeparam>
public class PagedApiResponseModel<TItem>
{
    /// <summary>
    /// 請求是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 業務回應碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 分頁項目清單（直接在頂層，不再巢狀於 Data）
    /// </summary>
    public IEnumerable<TItem> Items { get; set; } = Enumerable.Empty<TItem>();

    /// <summary>
    /// 當前頁碼（從 1 開始）
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每頁大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總筆數
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// 時間戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 追蹤 ID
    /// </summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// ApiResponse 建構輔助工廠，包含分頁回應建立方法
/// </summary>
public static class ApiResponseFactory
{
    /// <summary>
    /// 建立分頁成功回應，回傳 PagedApiResponseModel&lt;TItem&gt;，避免巢狀 Data.Items
    /// </summary>
    public static PagedApiResponseModel<TItem> CreatePagedSuccess<TItem>(
        IEnumerable<TItem>? items,
        int page,
        int pageSize,
        long total,
        string message = "操作成功",
        string code = "SUCCESS"
    )
    {
        return new PagedApiResponseModel<TItem>
        {
            Success = true,
            Code = code,
            Message = message,
            Items = items ?? Enumerable.Empty<TItem>(),
            Page = page,
            PageSize = pageSize,
            Total = total,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 建立分頁失敗回應（回傳空的分頁結構以利前端一致處理）
    /// </summary>
    public static PagedApiResponseModel<TItem> CreatePagedFailure<TItem>(
        string message,
        string code = "FAILURE"
    )
    {
        return new PagedApiResponseModel<TItem>
        {
            Success = false,
            Code = code,
            Message = message,
            Items = Enumerable.Empty<TItem>(),
            Page = 1,
            PageSize = 0,
            Total = 0,
            Timestamp = DateTime.UtcNow
        };
    }
}
