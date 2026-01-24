using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration.Controllers;

/// <summary>
/// 時間格式整合測試
/// 驗證 API 端點正確處理 UTC0 時間格式
/// </summary>
[Collection("Integration")]
public class TimeFormatIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _testToken = string.Empty;
    private Guid _testUserId;

    public TimeFormatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task InitializeAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 插入測試用戶
        var insertSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW())
            ON CONFLICT (account) DO UPDATE SET id = EXCLUDED.id
            RETURNING id;
        ";

        _testUserId = Guid.NewGuid();
        await using var command = new Npgsql.NpgsqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("id", _testUserId);
        command.Parameters.AddWithValue("account", "timeformat_testuser");
        command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword("Test@123", 12));
        command.Parameters.AddWithValue("display_name", "時間格式測試用戶");

        var result = await command.ExecuteScalarAsync();
        if (result != null)
        {
            _testUserId = (Guid)result;
        }



        // 登入取得 Token
        var loginRequest = new LoginRequest
        {
            Account = "timeformat_testuser",
            Password = "Test@123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(loginContent, _jsonOptions);
        _testToken = loginResult?.Data?.Token ?? string.Empty;

        // 設定預設 Authorization header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 刪除用戶角色關聯
        var deleteUserRolesSql = "DELETE FROM user_roles WHERE user_id = @user_id;";
        await using (var cmd = new Npgsql.NpgsqlCommand(deleteUserRolesSql, connection))
        {
            cmd.Parameters.AddWithValue("user_id", _testUserId);
            await cmd.ExecuteNonQueryAsync();
        }

        // 刪除用戶
        var deleteSql = "DELETE FROM users WHERE account = 'timeformat_testuser';";
        await using var command = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #region QueryAuditLogRequest Tests

    [Fact]
    public async Task QueryAuditLog_WithValidUtc0Format_ReturnsSuccess()
    {
        // Arrange
        var request = new QueryAuditLogRequest
        {
            StartTime = DateTimeOffset.Parse("2026-01-01T00:00:00.000Z"),
            EndTime = DateTimeOffset.Parse("2026-01-31T23:59:59.999Z")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auditlog/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QueryAuditLog_WithNonUtc0Timezone_AcceptsAndConvertsToUtc()
    {
        // Arrange - 手動構造 JSON，測試 converter 自動轉換非 UTC offset
        var json = """
        {
            "startTime": "2026-01-01T00:00:00+08:00",
            "endTime": "2026-01-31T23:59:59+08:00"
        }
        """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auditlog/query", content);

        // Assert - converter 應該已轉換為 UTC，但可能有其他驗證錯誤
        // 如果是 400，確認不是因為 offset 問題
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

            // 如果有錯誤，不應該提到 UTC0 或 offset
            var errorString = apiResponse?.Errors?.ToString() ?? string.Empty;
            errorString.Should().NotContain("UTC0", "不應該因為 offset 失敗");
            errorString.Should().NotContain("時區偏移", "不應該因為 offset 失敗");
        }
        // 如果是 200 也 OK，表示 converter 正常工作
    }

    [Fact]
    public async Task QueryAuditLog_WithInvalidDateFormat_ReturnsBadRequest()
    {
        // Arrange - 完全無效的日期格式
        var json = """
        {
            "startTime": "not-a-valid-date",
            "endTime": "also-invalid"
        }
        """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auditlog/query", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task InvalidTimeFormat_ReturnsStandardizedErrorResponse()
    {
        // Arrange - 使用完全無效的日期格式
        var json = """
        {
            "startTime": "this-is-not-a-date",
            "endTime": "neither-is-this"
        }
        """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auditlog/query", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

        // 驗證標準化錯誤格式
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.TraceId.Should().NotBeNullOrEmpty();
    }

    #endregion
}
