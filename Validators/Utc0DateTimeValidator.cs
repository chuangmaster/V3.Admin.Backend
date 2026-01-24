using FluentValidation;
using System.Text.RegularExpressions;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// FluentValidation 自訂驗證器擴充方法
/// 用於驗證 DateTimeOffset 是否符合 UTC0 ISO 8601 格式
/// </summary>
public static partial class Utc0DateTimeValidator
{
    // ISO 8601 UTC0 格式的正則表達式 (支援毫秒精度)
    // 格式: YYYY-MM-DDTHH:mm:ss.fffZ 或 YYYY-MM-DDTHH:mm:ssZ
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z$", RegexOptions.Compiled)]
    private static partial Regex Utc0FormatRegex();

    /// <summary>
    /// 驗證 DateTimeOffset 是否為 UTC0 時間
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeUtc0<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder)
    {
        return ruleBuilder
            .Must(dateTime => dateTime.Offset == TimeSpan.Zero)
            .WithMessage("日期時間必須為 UTC0 格式 (時區偏移必須為 +00:00)。預期格式: YYYY-MM-DDTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// 驗證可空的 DateTimeOffset 是否為 UTC0 時間
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> MustBeUtc0<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder)
    {
        return ruleBuilder
            .Must(dateTime => !dateTime.HasValue || dateTime.Value.Offset == TimeSpan.Zero)
            .WithMessage("日期時間必須為 UTC0 格式 (時區偏移必須為 +00:00)。預期格式: YYYY-MM-DDTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// 驗證字串是否為有效的 UTC0 ISO 8601 格式
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeUtc0IsoFormat<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(dateString =>
            {
                if (string.IsNullOrWhiteSpace(dateString))
                    return false;

                // 檢查格式是否符合 ISO 8601 UTC0
                if (!Utc0FormatRegex().IsMatch(dateString))
                    return false;

                // 嘗試解析以確保是有效的日期時間
                if (!DateTimeOffset.TryParse(dateString, out var dateTime))
                    return false;

                // 確保時區為 UTC
                return dateTime.Offset == TimeSpan.Zero;
            })
            .WithMessage("日期時間字串必須為有效的 UTC0 ISO 8601 格式。預期格式: YYYY-MM-DDTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// 驗證 DateTimeOffset 是否在合理的範圍內 (例如: 1900-01-01 到 2100-12-31)
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeInReasonableRange<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        DateTimeOffset? minDate = null,
        DateTimeOffset? maxDate = null)
    {
        var min = minDate ?? new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var max = maxDate ?? new DateTimeOffset(2100, 12, 31, 23, 59, 59, TimeSpan.Zero);

        return ruleBuilder
            .Must(dateTime => dateTime >= min && dateTime <= max)
            .WithMessage($"日期時間必須在 {min:yyyy-MM-dd} 到 {max:yyyy-MM-dd} 之間");
    }

    /// <summary>
    /// 驗證 DateTimeOffset 不能是未來時間
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustNotBeFutureDate<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder)
    {
        return ruleBuilder
            .Must(dateTime => dateTime <= DateTimeOffset.UtcNow)
            .WithMessage("日期時間不能是未來時間");
    }

    /// <summary>
    /// 驗證 DateTimeOffset 必須是未來時間
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeFutureDate<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder)
    {
        return ruleBuilder
            .Must(dateTime => dateTime > DateTimeOffset.UtcNow)
            .WithMessage("日期時間必須是未來時間");
    }
}
