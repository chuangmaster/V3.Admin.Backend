using System.Text.Json;
using System.Text.Json.Serialization;

namespace V3.Admin.Backend.Converters;

/// <summary>
/// 自訂 JSON 轉換器,強制所有 DateTimeOffset 序列化/反序列化為 UTC0 格式
/// 格式: YYYY-MM-DDTHH:mm:ss.fffZ (毫秒精度)
/// </summary>
public class Utc0DateTimeJsonConverter : JsonConverter<DateTimeOffset>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>
    /// 讀取 JSON 字串並轉換為 DateTimeOffset (UTC)
    /// </summary>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new JsonException("日期時間值不能為空");
        }

        // 嘗試解析為 DateTimeOffset
        if (!DateTimeOffset.TryParse(dateString, out var dateTime))
        {
            throw new JsonException($"無效的日期時間格式。預期格式: {DateTimeFormat}");
        }

        // 確保時區為 UTC,如果不是則拋出異常
        if (dateTime.Offset != TimeSpan.Zero)
        {
            throw new JsonException($"日期時間必須為 UTC0 格式 (時區偏移為 +00:00)。收到的偏移: {dateTime.Offset}");
        }

        return dateTime.ToUniversalTime();
    }

    /// <summary>
    /// 將 DateTimeOffset 序列化為 UTC0 格式的 JSON 字串
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // 確保轉換為 UTC 並使用固定格式
        var utcValue = value.ToUniversalTime();
        writer.WriteStringValue(utcValue.ToString(DateTimeFormat));
    }
}
