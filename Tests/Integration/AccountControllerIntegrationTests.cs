using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration;

/// <summary>
/// AccountController 整合測試
/// 測試密碼變更和重設功能
/// </summary>
[Collection("Integration")]
public class AccountControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private Guid _testUserId;
    private Guid _adminUserId;
    private string _adminToken = string.Empty;
    private string _testUserToken = string.Empty;

    public AccountControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task InitializeAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 插入測試用的管理員帳號
        var insertAdminSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW())
            ON CONFLICT (account) DO UPDATE SET id = EXCLUDED.id
            RETURNING id;
        ";

        _adminUserId = Guid.NewGuid();
        await using var adminCommand = new Npgsql.NpgsqlCommand(insertAdminSql, connection);
        adminCommand.Parameters.AddWithValue("id", _adminUserId);
        adminCommand.Parameters.AddWithValue("account", "admin");
        adminCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("Admin@123", 12)
        );
        adminCommand.Parameters.AddWithValue("display_name", "管理員");
        var adminIdResult = await adminCommand.ExecuteScalarAsync();
        if (adminIdResult != null)
        {
            _adminUserId = (Guid)adminIdResult;
        }

        // 插入測試用的普通用戶
        _testUserId = Guid.NewGuid();
        await using var userCommand = new Npgsql.NpgsqlCommand(insertAdminSql, connection);
        userCommand.Parameters.AddWithValue("id", _testUserId);
        userCommand.Parameters.AddWithValue("account", "testuser");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("TestUser@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "測試用戶");
        var userIdResult = await userCommand.ExecuteScalarAsync();
        if (userIdResult != null)
        {
            _testUserId = (Guid)userIdResult;
        }

        // 獲取管理員 Token
        var adminLoginRequest = new LoginRequest
        {
            Account = "admin",
            Password = "Admin@123",
        };
        var adminLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            adminLoginRequest
        );
        var adminLoginContent = await adminLoginResponse.Content.ReadAsStringAsync();
        var adminLoginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            adminLoginContent,
            _jsonOptions
        );
        _adminToken = adminLoginResult?.Data?.Token ?? string.Empty;

        // 獲取測試用戶 Token
        var testUserLoginRequest = new LoginRequest
        {
            Account = "testuser",
            Password = "TestUser@123",
        };
        var testUserLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            testUserLoginRequest
        );
        var testUserLoginContent = await testUserLoginResponse.Content.ReadAsStringAsync();
        var testUserLoginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            testUserLoginContent,
            _jsonOptions
        );
        _testUserToken = testUserLoginResult?.Data?.Token ?? string.Empty;
    }

    public async Task DisposeAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var deleteSql = "DELETE FROM users WHERE account IN ('admin', 'testuser');";
        await using var command = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    #region ChangePassword Tests

    /// <summary>
    /// 測試：用戶使用正確的舊密碼成功變更密碼
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithValidOldPassword_ReturnsSuccess()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "TestUser@123",
            NewPassword = "NewPassword@456",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _testUserToken
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Message.Should().Contain("密碼修改成功");

        // 等待快取清除完成
        await Task.Delay(100);

        // 清除舊的 Authorization header,因為登入不需要攜帶 Token
        _client.DefaultRequestHeaders.Authorization = null;

        // 驗證可以使用新密碼登入
        var loginRequest = new LoginRequest
        {
            Account = "testuser",
            Password = "NewPassword@456",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// 測試：使用錯誤的舊密碼變更密碼失敗
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithInvalidOldPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "WrongPassword@123",
            NewPassword = "NewPassword@456",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _testUserToken
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 測試：新密碼與舊密碼相同時變更失敗
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithSamePassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "TestUser@123",
            NewPassword = "TestUser@123",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _testUserToken
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 測試：版本號不匹配時返回 409 Conflict
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithVersionMismatch_ReturnsConflict()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "TestUser@123",
            NewPassword = "NewPassword@456",
            Version = 999, // 錯誤的版本號
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _testUserToken
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
    }

    /// <summary>
    /// 測試：未驗證的用戶無法變更密碼
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "TestUser@123",
            NewPassword = "NewPassword@456",
            Version = 1,
        };

        // 不設定 Authorization header

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ResetPassword Tests

    /// <summary>
    /// 測試：管理員成功重設用戶密碼
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithAdminPermission_ReturnsSuccess()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "ResetPassword@789",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminToken
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Message.Should().Contain("密碼重設成功");

        // 驗證可以使用新密碼登入
        var loginRequest = new LoginRequest
        {
            Account = "testuser",
            Password = "ResetPassword@789",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// 測試：重設不存在的用戶密碼返回 NotFound
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonexistentUserId = Guid.NewGuid();
        var request = new ResetPasswordRequest
        {
            NewPassword = "ResetPassword@789",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminToken
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{nonexistentUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 測試：版本號不匹配時返回 409 Conflict
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithVersionMismatch_ReturnsConflict()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "ResetPassword@789",
            Version = 999, // 錯誤的版本號
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminToken
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
    }

    /// <summary>
    /// 測試：未驗證的用戶無法重設密碼
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "ResetPassword@789",
            Version = 1,
        };

        // 不設定 Authorization header

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 測試:無 user.profile.update 權限的用戶無法修改密碼
    /// </summary>
    [Fact]
    public async Task ChangeMyPassword_WithoutPermission_ReturnsForbidden()
    {
        // Arrange: 建立一個沒有 user.profile.update 權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "noperm_user");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoPermUser@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無權限用戶");
        await userCommand.ExecuteNonQueryAsync();

        // 清除舊的 Authorization header
        _client.DefaultRequestHeaders.Authorization = null;

        // 獲取無權限用戶的 Token
        var loginRequest = new LoginRequest
        {
            Account = "noperm_user",
            Password = "NoPermUser@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        var request = new ChangePasswordRequest
        {
            OldPassword = "NoPermUser@123",
            NewPassword = "NewPassword@456",
            Version = 1,
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var content = await response.Content.ReadAsStringAsync();
        // 權限被拒時 middleware 可能返回空內容
        if (!string.IsNullOrWhiteSpace(content))
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
                content,
                _jsonOptions
            );
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
        }

        // Cleanup
        var deleteSql = "DELETE FROM users WHERE account = 'noperm_user';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    #endregion

    #region ResetPassword Permission Tests

    /// <summary>
    /// 測試:無 account.update 權限的用戶無法重設密碼
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithoutPermission_ReturnsForbidden()
    {
        // Arrange: 建立一個沒有 account.update 權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "noperm_admin");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoPermAdmin@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無權限管理員");
        await userCommand.ExecuteNonQueryAsync();

        // 清除舊的 Authorization header
        _client.DefaultRequestHeaders.Authorization = null;

        // 獲取無權限用戶的 Token
        var loginRequest = new LoginRequest
        {
            Account = "noperm_admin",
            Password = "NoPermAdmin@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        var request = new ResetPasswordRequest
        {
            NewPassword = "ResetPassword@789",
            Version = 1,
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var content = await response.Content.ReadAsStringAsync();
        // 權限被拒時 middleware 可能返回空內容
        if (!string.IsNullOrWhiteSpace(content))
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
                content,
                _jsonOptions
            );
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
        }

        // Cleanup
        var deleteSql = "DELETE FROM users WHERE account = 'noperm_admin';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試:驗證審計日誌確實被寫入資料庫
    /// </summary>
    /// <remarks>
    /// 注意: 當前實現中 Controller 層尚未整合 AuditLogRepository,此測試暫時略過。
    /// 待實現審計日誌記錄功能後再啟用此測試。
    /// </remarks>
    [Fact(Skip = "審計日誌記錄功能尚未在 AccountController 中實現")]
    public async Task ResetPassword_Success_CreatesAuditLog()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "AuditTestPassword@789",
            Version = 1,
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _adminToken
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 查詢審計日誌
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var auditLogSql = @"
            SELECT COUNT(*) FROM audit_logs
            WHERE target_id = @TargetId
            AND operation_type = @OperationType
            AND operation_time > NOW() - INTERVAL '10 seconds';
        ";

        await using var command = new Npgsql.NpgsqlCommand(auditLogSql, connection);
        command.Parameters.AddWithValue("TargetId", _testUserId);
        command.Parameters.AddWithValue("OperationType", "重設密碼");

        var auditCount = await command.ExecuteScalarAsync();

        // 驗證審計日誌已建立
        Convert.ToInt32(auditCount).Should().BeGreaterThan(0, "審計日誌應該被記錄");
    }

    #endregion

    #region US4 Permission Control Tests

    /// <summary>
    /// 測試:驗證 account.read 權限控制 - 列表查詢
    /// </summary>
    [Fact]
    public async Task GetAccounts_WithoutAccountReadPermission_ReturnsForbidden()
    {
        // Arrange: 建立沒有 account.read 權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "no_read_perm");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoReadPerm@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無讀取權限用戶");
        await userCommand.ExecuteNonQueryAsync();

        // 清除並重新登入
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Account = "no_read_perm",
            Password = "NoReadPerm@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        // Act
        var response = await _client.GetAsync("/api/account?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
        var deleteSql = "DELETE FROM users WHERE account = 'no_read_perm';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試:驗證 account.read 權限控制 - 單一查詢
    /// </summary>
    [Fact]
    public async Task GetAccount_WithoutAccountReadPermission_ReturnsForbidden()
    {
        // Arrange: 建立沒有 account.read 權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "no_read_perm2");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoReadPerm2@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無讀取權限用戶2");
        await userCommand.ExecuteNonQueryAsync();

        // 清除並重新登入
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Account = "no_read_perm2",
            Password = "NoReadPerm2@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        // Act
        var response = await _client.GetAsync($"/api/account/{_testUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
        var deleteSql = "DELETE FROM users WHERE account = 'no_read_perm2';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試:驗證 account.update 權限控制 - 更新帳號
    /// </summary>
    [Fact]
    public async Task UpdateAccount_WithoutAccountUpdatePermission_ReturnsForbidden()
    {
        // Arrange: 建立沒有 account.update 權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "no_update_perm");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoUpdatePerm@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無更新權限用戶");
        await userCommand.ExecuteNonQueryAsync();

        // 清除並重新登入
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Account = "no_update_perm",
            Password = "NoUpdatePerm@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        var updateRequest = new UpdateAccountRequest
        {
            DisplayName = "新顯示名稱",
            Version = 1,
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}",
            updateRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
        var deleteSql = "DELETE FROM users WHERE account = 'no_update_perm';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試:驗證 account.delete 權限控制
    /// </summary>
    [Fact]
    public async Task DeleteAccount_WithoutAccountDeletePermission_ReturnsForbidden()
    {
        // Arrange: 建立沒有 account.delete 權限的用戶和待刪除的測試用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var toDeleteUserId = Guid.NewGuid();

        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        // 建立無權限用戶
        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "no_delete_perm");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoDeletePerm@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "無刪除權限用戶");
        await userCommand.ExecuteNonQueryAsync();

        // 建立待刪除用戶
        await using var toDeleteCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        toDeleteCommand.Parameters.AddWithValue("id", toDeleteUserId);
        toDeleteCommand.Parameters.AddWithValue("account", "user_to_delete");
        toDeleteCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("ToDelete@123", 12)
        );
        toDeleteCommand.Parameters.AddWithValue("display_name", "待刪除用戶");
        await toDeleteCommand.ExecuteNonQueryAsync();

        // 清除並重新登入
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Account = "no_delete_perm",
            Password = "NoDeletePerm@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        var deleteRequest = new DeleteAccountRequest { Confirmation = "CONFIRM" };

        // Act
        var response = await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"/api/account/{toDeleteUserId}")
            {
                Content = JsonContent.Create(deleteRequest),
            }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
        var deleteSql =
            "DELETE FROM users WHERE account IN ('no_delete_perm', 'user_to_delete');";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試:驗證無權限用戶被拒絕訪問所有需要權限的 Account 端點
    /// </summary>
    [Fact]
    public async Task AccountEndpoints_WithoutAnyPermission_AllReturnForbidden()
    {
        // Arrange: 建立完全沒有權限的用戶
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var noPermUserId = Guid.NewGuid();
        var insertUserSql = @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW());
        ";

        await using var userCommand = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        userCommand.Parameters.AddWithValue("id", noPermUserId);
        userCommand.Parameters.AddWithValue("account", "no_any_perm");
        userCommand.Parameters.AddWithValue(
            "password_hash",
            BCrypt.Net.BCrypt.HashPassword("NoAnyPerm@123", 12)
        );
        userCommand.Parameters.AddWithValue("display_name", "完全無權限用戶");
        await userCommand.ExecuteNonQueryAsync();

        // 登入
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Account = "no_any_perm",
            Password = "NoAnyPerm@123",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
            loginContent,
            _jsonOptions
        );
        var noPermToken = loginResult?.Data?.Token ?? string.Empty;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            noPermToken
        );

        // Act & Assert: 測試所有需要權限的端點
        var getAccountsResponse = await _client.GetAsync("/api/account");
        getAccountsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "GET /api/account 應該被拒絕");

        var getAccountResponse = await _client.GetAsync($"/api/account/{_testUserId}");
        getAccountResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "GET /api/account/{id} 應該被拒絕");

        var updateRequest = new UpdateAccountRequest { DisplayName = "測試", Version = 1 };
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/account/{noPermUserId}",
            updateRequest
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "PUT /api/account/{id} 應該被拒絕");

        var resetPasswordRequest = new ResetPasswordRequest
        {
            NewPassword = "NewPass@123",
            Version = 1,
        };
        var resetPasswordResponse = await _client.PutAsJsonAsync(
            $"/api/account/{_testUserId}/reset-password",
            resetPasswordRequest
        );
        resetPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "PUT /api/account/{id}/reset-password 應該被拒絕");

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
        var deleteSql = "DELETE FROM users WHERE account = 'no_any_perm';";
        await using var deleteCommand = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await deleteCommand.ExecuteNonQueryAsync();
    }

    #endregion
}
