using System.Text.Json;
using FluentAssertions;
using V3.Admin.Backend.Converters;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Converters;

/// <summary>
/// Utc0DateTimeJsonConverter 的單元測試
/// 測試 DateTimeOffset 的 JSON 序列化/反序列化行為
/// </summary>
public class Utc0DateTimeJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public Utc0DateTimeJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new Utc0DateTimeJsonConverter() }
        };
    }

    #region 序列化測試 (Write)

    [Fact]
    public void Serialize_ValidUtcDateTime_ReturnsCorrectFormat()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2026, 1, 24, 6, 0, 0, 123, TimeSpan.Zero);
        var testObject = new { CreatedAt = dateTime };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        json.Should().Contain("\"2026-01-24T06:00:00.123Z\"");
    }

    [Fact]
    public void Serialize_NonUtcDateTime_ConvertsToUtc()
    {
        // Arrange - 台北時間 (UTC+8) 14:00
        var taipeiTime = new DateTimeOffset(2026, 1, 24, 14, 0, 0, 0, TimeSpan.FromHours(8));
        var testObject = new { CreatedAt = taipeiTime };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert - 應轉換為 UTC 06:00
        json.Should().Contain("\"2026-01-24T06:00:00.000Z\"");
    }

    [Fact]
    public void Serialize_DateTimeWithMilliseconds_PreservesMilliseconds()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2026, 1, 24, 8, 30, 15, 456, TimeSpan.Zero);
        var testObject = new { UpdatedAt = dateTime };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        json.Should().Contain("\"2026-01-24T08:30:15.456Z\"");
    }

    [Fact]
    public void Serialize_MinValue_HandlesEdgeCase()
    {
        // Arrange
        var dateTime = DateTimeOffset.MinValue;
        var testObject = new { CreatedAt = dateTime };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        json.Should().Contain("\"0001-01-01T00:00:00.000Z\"");
    }

    [Fact]
    public void Serialize_MaxValue_HandlesEdgeCase()
    {
        // Arrange
        var dateTime = DateTimeOffset.MaxValue;
        var testObject = new { CreatedAt = dateTime };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);

        // Assert
        json.Should().Contain("\"9999-12-31T23:59:59.999Z\"");
    }

    #endregion

    #region 反序列化測試 (Read)

    [Fact]
    public void Deserialize_ValidUtc0Format_ReturnsCorrectDateTime()
    {
        // Arrange
        var json = "{\"CreatedAt\":\"2026-01-24T06:00:00.123Z\"}";

        // Act
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.Year.Should().Be(2026);
        result.CreatedAt.Month.Should().Be(1);
        result.CreatedAt.Day.Should().Be(24);
        result.CreatedAt.Hour.Should().Be(6);
        result.CreatedAt.Minute.Should().Be(0);
        result.CreatedAt.Second.Should().Be(0);
        result.CreatedAt.Millisecond.Should().Be(123);
        result.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Deserialize_ValidUtc0FormatWithoutMilliseconds_AcceptsFormat()
    {
        // Arrange
        var json = "{\"CreatedAt\":\"2026-01-24T06:00:00Z\"}";

        // Act
        var result = JsonSerializer.Deserialize<TestModel>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.Millisecond.Should().Be(0);
        result.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Deserialize_NonUtc0Format_ThrowsJsonException()
    {
        // Arrange - 包含時區偏移 +08:00
        var json = "{\"CreatedAt\":\"2026-01-24T14:00:00+08:00\"}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<TestModel>(json, _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*UTC0*");
    }

    [Fact]
    public void Deserialize_InvalidDateFormat_ThrowsJsonException()
    {
        // Arrange - 使用完全無效的格式
        var json = "{\"CreatedAt\":\"invalid-date-string\"}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<TestModel>(json, _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*無效的日期時間格式*");
    }

    [Fact]
    public void Deserialize_EmptyString_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"CreatedAt\":\"\"}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<TestModel>(json, _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*不能為空*");
    }

    [Fact]
    public void Deserialize_NullValue_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"CreatedAt\":null}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<TestModel>(json, _options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_InvalidString_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"CreatedAt\":\"not-a-date\"}";

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<TestModel>(json, _options);
        act.Should().Throw<JsonException>()
            .WithMessage("*無效的日期時間格式*");
    }

    #endregion

    #region 往返測試 (Round-trip)

    [Fact]
    public void RoundTrip_SerializeAndDeserialize_PreservesValue()
    {
        // Arrange
        var original = new TestModel
        {
            CreatedAt = new DateTimeOffset(2026, 1, 24, 8, 30, 15, 789, TimeSpan.Zero)
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TestModel>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CreatedAt.Should().Be(original.CreatedAt);
    }

    [Fact]
    public void RoundTrip_WithNonUtcInput_ConvertsAndPreservesUtcTime()
    {
        // Arrange - 紐約時間 (UTC-5) 10:00
        var newYorkTime = new DateTimeOffset(2026, 1, 24, 10, 0, 0, 0, TimeSpan.FromHours(-5));
        var original = new TestModel { CreatedAt = newYorkTime };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<TestModel>(json, _options);

        // Assert - 應保留 UTC 時間 15:00
        deserialized.Should().NotBeNull();
        deserialized!.CreatedAt.Hour.Should().Be(15); // UTC 15:00
        deserialized.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region 測試輔助類別

    private class TestModel
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    #endregion
}
