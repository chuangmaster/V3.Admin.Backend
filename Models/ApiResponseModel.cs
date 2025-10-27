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
    public T? Data { get; set; }

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
