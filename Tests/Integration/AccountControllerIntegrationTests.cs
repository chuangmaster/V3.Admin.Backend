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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    #endregion
}
