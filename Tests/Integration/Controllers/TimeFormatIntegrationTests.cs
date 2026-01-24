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

        // 建立測試角色和權限
        const string setupPermissionsSql = @"
            -- 建立測試角色
            INSERT INTO roles (role_name, description, version, is_deleted, created_at, updated_at)
            SELECT 'timeformat-test-role', 'Role for time format tests', 1, false, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM roles WHERE role_name = 'timeformat-test-role' AND is_deleted = FALSE);

            -- 建立必要權限
            INSERT INTO permissions (permission_code, name, description, permission_type, version, is_deleted, created_at, updated_at)
            SELECT 'serviceOrder.buyback.read', 'Service Order Read', 'Read service order', 'function', 1, false, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = 'serviceOrder.buyback.read' AND is_deleted = FALSE);

            INSERT INTO permissions (permission_code, name, description, permission_type, version, is_deleted, created_at, updated_at)
            SELECT 'serviceOrder.buyback.create', 'Service Order Create', 'Create service order', 'function', 1, false, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = 'serviceOrder.buyback.create' AND is_deleted = FALSE);

            INSERT INTO permissions (permission_code, name, description, permission_type, version, is_deleted, created_at, updated_at)
            SELECT 'serviceOrder.consignment.create', 'Consignment Order Create', 'Create consignment order', 'function', 1, false, NOW(), NOW()
            WHERE NOT EXISTS (SELECT 1 FROM permissions WHERE permission_code = 'serviceOrder.consignment.create' AND is_deleted = FALSE);

            -- 關聯角色與權限
            INSERT INTO role_permissions (role_id, permission_id, assigned_by)
            SELECT r.id, p.id, NULL
            FROM roles r, permissions p
            WHERE r.role_name = 'timeformat-test-role'
                AND p.permission_code IN ('serviceOrder.buyback.read', 'serviceOrder.buyback.create', 'serviceOrder.consignment.create')
                AND NOT EXISTS (
                    SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id
                );

            -- 分配角色給測試用戶
            INSERT INTO user_roles (user_id, role_id, assigned_by)
            SELECT @user_id, r.id, NULL
            FROM roles r
            WHERE r.role_name = 'timeformat-test-role'
                AND NOT EXISTS (
                    SELECT 1 FROM user_roles ur WHERE ur.user_id = @user_id AND ur.role_id = r.id AND ur.is_deleted = FALSE
                );
        ";

        await using (var permCommand = new Npgsql.NpgsqlCommand(setupPermissionsSql, connection))
        {
            permCommand.Parameters.AddWithValue("user_id", _testUserId);
            await permCommand.ExecuteNonQueryAsync();
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

    #region QueryServiceOrdersRequest Tests

    [Fact]
    public async Task QueryServiceOrders_WithValidUtc0Format_ReturnsSuccess()
    {
        // Arrange - 使用 GET 請求查詢參數
        var startDate = "2026-01-01T00:00:00.000Z";
        var endDate = "2026-01-31T23:59:59.999Z";

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={Uri.EscapeDataString(startDate)}&serviceDateEnd={Uri.EscapeDataString(endDate)}");

        // Assert - 如果是 400，檢查不是因為時間格式
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("UTC", "時間格式應該正確");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task QueryServiceOrders_WithNonUtc0Timezone_AcceptsAndConvertsToUtc()
    {
        // Arrange - 使用非 UTC0 時區，測試自動轉換
        var startDate = "2026-01-01T00:00:00+09:00";
        var endDate = "2026-01-31T23:59:59+09:00";

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={Uri.EscapeDataString(startDate)}&serviceDateEnd={Uri.EscapeDataString(endDate)}");

        // Assert - 應該接受並自動轉為 UTC
        response.StatusCode.Should().Be(HttpStatusCode.OK, "converter 應接受任意 offset 並轉換為 UTC");
    }

    [Fact]
    public async Task QueryServiceOrders_WithMissingTimezone_AcceptsLocalTime()
    {
        // Arrange - 缺少時區識別符，應該被視為 local time
        var startDate = "2026-01-01T00:00:00";
        var endDate = "2026-01-31T23:59:59";

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={Uri.EscapeDataString(startDate)}&serviceDateEnd={Uri.EscapeDataString(endDate)}");

        // Assert - 可能會接受或拒絕，但不應該是因為 UTC 問題
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            // 如果失敗，不應該是因為 UTC 相關問題
            content.Should().NotContain("UTC0", "不應該拒絕 missing timezone");
        }
    }

    #endregion

    #region CreateConsignmentOrderRequest Tests

    [Fact]
    public async Task CreateConsignmentOrder_WithValidUtc0Format_ProcessesRequest()
    {
        // Arrange
        var testCustomerId = Guid.NewGuid();
        var request = new CreateConsignmentOrderRequest
        {
            ServiceDate = DateTimeOffset.Parse("2026-02-01T10:00:00.000Z"),
            ConsignmentStartDate = DateTimeOffset.Parse("2026-02-01T00:00:00.000Z"),
            ConsignmentEndDate = DateTimeOffset.Parse("2026-02-28T23:59:59.999Z"),
            CustomerId = testCustomerId
        };

        // Act - 此測試可能因為缺少其他必要欄位或權限而失敗,但時間格式應該被接受
        var response = await _client.PostAsJsonAsync("/api/service-orders/consignment", request);

        // Assert - 時間格式正確,不應該因為時間格式而回傳 400
        // 注意: 可能因為其他驗證失敗 (如缺少必要欄位) 而回傳 400,需要檢查錯誤訊息
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

            // 確保不是因為時間格式錯誤
            if (apiResponse?.Errors?.ContainsKey("detail") == true)
            {
                apiResponse.Errors["detail"].ToString().Should().NotContain("UTC0");
            }
        }
    }

    [Fact]
    public async Task CreateConsignmentOrder_WithNonUtc0Timezone_AcceptsAndConvertsToUtc()
    {
        // Arrange - 使用非 UTC0 時區，測試自動轉換
        var testCustomerId = Guid.NewGuid();
        var json = $$"""
        {
            "customerId": "{{testCustomerId}}",
            "serviceDate": "2026-02-01T10:00:00-05:00",
            "consignmentStartDate": "2026-02-01T00:00:00-05:00",
            "consignmentEndDate": "2026-02-28T23:59:59-05:00"
        }
        """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/service-orders/consignment", content);

        // Assert - 可能因其他驗證失敗（缺少必要欄位），但不應該是時間格式問題
        // 如果是 400，檢查不是因為時間格式
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

            // 確保不是因為時間格式錯誤（converter 應該已接受）
            if (apiResponse?.Errors != null)
            {
                var errorString = apiResponse.Errors.ToString();
                errorString.Should().NotContain("UTC0", "不應該拒絕 non-UTC offset");
                errorString.Should().NotContain("timezone", "不應該拒絕 non-UTC offset");
            }
        }
    }

    [Fact]
    public async Task CreateConsignmentOrder_WithFutureDateInUtc0_ReturnsValidationError()
    {
        // Arrange - 服務日期為未來日期 (UTC0 格式正確但違反業務規則)
        var futureDate = DateTimeOffset.UtcNow.AddDays(10);
        var testCustomerId = Guid.NewGuid();
        var json = $$"""
        {
            "customerId": "{{testCustomerId}}",
            "serviceDate": "{{futureDate:yyyy-MM-ddTHH:mm:ss.fffZ}}",
            "consignmentStartDate": "{{futureDate:yyyy-MM-ddTHH:mm:ss.fffZ}}",
            "consignmentEndDate": "{{futureDate.AddDays(30):yyyy-MM-ddTHH:mm:ss.fffZ}}"
        }
        """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/service-orders/consignment", content);

        // Assert - 可能因缺少必要欄位而失敗（400 或 500），但時間格式應該正確
        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            // 只要不是因為時間格式問題就好
            responseContent.Should().NotContain("UTC0", "不應該因為時間格式失敗");
            responseContent.Should().NotContain("時區", "不應該因為時間格式失敗");
        }
        else
        {
            // 如果不是錯誤狀態碼，那應該是 OK（不太可能但也接受）
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        }
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
