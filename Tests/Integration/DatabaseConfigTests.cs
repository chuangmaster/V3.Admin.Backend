using System.Data;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration;

/// <summary>
/// 資料庫配置整合測試,驗證 PostgreSQL UTC 時區設定與 Dapper 時間參數處理
/// </summary>
public class DatabaseConfigTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private IDbConnection? _connection;

    /// <summary>
    /// 測試前初始化 PostgreSQL 容器
    /// </summary>
    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        await _postgres.StartAsync();

        // 建立帶有 UTC 時區參數的連線
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Timezone = "UTC"
        };

        _connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        _connection.Open();

        // 建立測試用表
        await _connection.ExecuteAsync(
            """
            CREATE TABLE test_timestamps (
                id SERIAL PRIMARY KEY,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NULL
            );
            """
        );
    }

    /// <summary>
    /// 測試後清理資源
    /// </summary>
    public async Task DisposeAsync()
    {
        _connection?.Dispose();

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public void ConnectionString_ShouldIncludeTimezoneParameter()
    {
        // Arrange & Act
        var npgsqlConnection = _connection as NpgsqlConnection;
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(npgsqlConnection!.ConnectionString);

        // Assert
        connectionStringBuilder.Timezone.Should().Be("UTC", "連線字串應包含 Timezone=UTC 參數");
    }

    [Fact]
    public async Task PostgreSql_ShouldUseUtcTimezone()
    {
        // Arrange & Act
        var timezone = await _connection!.QuerySingleAsync<string>("SHOW TIMEZONE");

        // Assert
        timezone.Should().Be("UTC", "PostgreSQL 連線應使用 UTC 時區");
    }

    [Fact]
    public async Task Dapper_ShouldHandleDateTimeOffsetParameters_WithUtcOffset()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;

        // Act
        var id = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, @UpdatedAt) RETURNING id",
            new { CreatedAt = utcNow, UpdatedAt = utcNow }
        );

        var result = await _connection.QuerySingleAsync<dynamic>(
            "SELECT created_at, updated_at FROM test_timestamps WHERE id = @Id",
            new { Id = id }
        );

        // Assert
        id.Should().BeGreaterThan(0);

        DateTimeOffset createdAt = result.created_at;
        DateTimeOffset updatedAt = result.updated_at;

        createdAt.Offset.Should().Be(TimeSpan.Zero, "從資料庫讀取的時間應為 UTC+0 偏移");
        updatedAt.Offset.Should().Be(TimeSpan.Zero, "從資料庫讀取的時間應為 UTC+0 偏移");

        // 允許 1 秒誤差 (資料庫精度)
        createdAt.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(1));
        updatedAt.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Dapper_ShouldHandleNullableDateTimeOffsetParameters()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset? nullValue = null;

        // Act
        var id = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, @UpdatedAt) RETURNING id",
            new { CreatedAt = utcNow, UpdatedAt = nullValue }
        );

        var result = await _connection.QuerySingleAsync<dynamic>(
            "SELECT created_at, updated_at FROM test_timestamps WHERE id = @Id",
            new { Id = id }
        );

        // Assert
        DateTimeOffset createdAt = result.created_at;
        object updatedAt = result.updated_at;

        createdAt.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(1));
        updatedAt.Should().Be(DBNull.Value, "NULL 值應正確處理");
    }

    [Fact]
    public async Task Dapper_ShouldConvertNonUtcToUtc_WhenInserting()
    {
        // Arrange - 建立台北時區時間 (UTC+8)
        var taipeiTime = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(8));
        var expectedUtcTime = taipeiTime.ToUniversalTime();

        // Act
        var id = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, @UpdatedAt) RETURNING id",
            new { CreatedAt = taipeiTime, UpdatedAt = taipeiTime }
        );

        var result = await _connection.QuerySingleAsync<dynamic>(
            "SELECT created_at, updated_at FROM test_timestamps WHERE id = @Id",
            new { Id = id }
        );

        // Assert
        DateTimeOffset createdAt = result.created_at;
        DateTimeOffset updatedAt = result.updated_at;

        createdAt.Offset.Should().Be(TimeSpan.Zero, "資料庫應將時間轉換為 UTC+0");
        updatedAt.Offset.Should().Be(TimeSpan.Zero, "資料庫應將時間轉換為 UTC+0");

        createdAt.Should().BeCloseTo(expectedUtcTime, TimeSpan.FromSeconds(1));
        updatedAt.Should().BeCloseTo(expectedUtcTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Dapper_ShouldPreserveMilliseconds_WhenStoringTimestamps()
    {
        // Arrange
        var exactTime = new DateTimeOffset(2024, 1, 15, 10, 30, 45, 123, TimeSpan.Zero);

        // Act
        var id = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, NULL) RETURNING id",
            new { CreatedAt = exactTime }
        );

        var result = await _connection.QuerySingleAsync<DateTimeOffset>(
            "SELECT created_at FROM test_timestamps WHERE id = @Id",
            new { Id = id }
        );

        // Assert
        result.Year.Should().Be(2024);
        result.Month.Should().Be(1);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(10);
        result.Minute.Should().Be(30);
        result.Second.Should().Be(45);
        result.Millisecond.Should().Be(123, "PostgreSQL TIMESTAMPTZ 應保留毫秒精度");
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Dapper_ShouldHandleMinAndMaxDateTimeOffsetValues()
    {
        // Arrange
        var minValue = DateTimeOffset.MinValue.ToUniversalTime();
        var maxValue = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act & Assert - Min Value
        var minId = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, NULL) RETURNING id",
            new { CreatedAt = minValue }
        );

        var minResult = await _connection.QuerySingleAsync<DateTimeOffset>(
            "SELECT created_at FROM test_timestamps WHERE id = @Id",
            new { Id = minId }
        );

        minResult.Offset.Should().Be(TimeSpan.Zero);
        minResult.Year.Should().Be(1);

        // Act & Assert - Max Value
        var maxId = await _connection!.ExecuteScalarAsync<int>(
            "INSERT INTO test_timestamps (created_at, updated_at) VALUES (@CreatedAt, NULL) RETURNING id",
            new { CreatedAt = maxValue }
        );

        var maxResult = await _connection.QuerySingleAsync<DateTimeOffset>(
            "SELECT created_at FROM test_timestamps WHERE id = @Id",
            new { Id = maxId }
        );

        maxResult.Offset.Should().Be(TimeSpan.Zero);
        maxResult.Year.Should().Be(9999);
        maxResult.Month.Should().Be(12);
        maxResult.Day.Should().Be(31);
    }

    [Fact]
    public async Task Dapper_ShouldSupportWhereClauseWithDateTimeOffset()
    {
        // Arrange
        var time1 = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 1, 16, 10, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2024, 1, 17, 10, 0, 0, TimeSpan.Zero);

        await _connection!.ExecuteAsync(
            """
            INSERT INTO test_timestamps (created_at, updated_at)
            VALUES (@Time1, NULL), (@Time2, NULL), (@Time3, NULL)
            """,
            new { Time1 = time1, Time2 = time2, Time3 = time3 }
        );

        // Act - 查詢在 time1 和 time3 之間的記錄
        var results = await _connection.QueryAsync<DateTimeOffset>(
            """
            SELECT created_at
            FROM test_timestamps
            WHERE created_at >= @StartTime AND created_at <= @EndTime
            ORDER BY created_at
            """,
            new { StartTime = time1, EndTime = time3 }
        );

        // Assert
        var resultList = results.ToList();
        resultList.Should().HaveCount(3);
        resultList[0].Should().BeCloseTo(time1, TimeSpan.FromSeconds(1));
        resultList[1].Should().BeCloseTo(time2, TimeSpan.FromSeconds(1));
        resultList[2].Should().BeCloseTo(time3, TimeSpan.FromSeconds(1));
    }
}
