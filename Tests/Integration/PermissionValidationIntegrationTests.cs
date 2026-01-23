using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration;

/// <summary>
/// 權限驗證系統集成測試
/// 測試多角色權限合併、失敗記錄、中介軟體驗證流程
/// </summary>
[Collection("Integration")]
public class PermissionValidationIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private Guid _testUserId = Guid.Empty;
    private Guid _testRoleId1 = Guid.Empty;
    private Guid _testRoleId2 = Guid.Empty;
    private Guid _testPermissionId1 = Guid.Empty;
    private Guid _testPermissionId2 = Guid.Empty;
    private string _testPermissionCode1 = string.Empty;
    private string _testPermissionCode2 = string.Empty;
    private string? _testToken;
    private string _testUsername = string.Empty;
    private const string _testPassword = "TestPass@123";

    public PermissionValidationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// 初始化測試環境：建立測試用戶、角色、權限
    /// </summary>
    public async Task InitializeAsync()
    { // 產生唯一 account (符合 VARCHAR(20) 且保留 _test_user 後綴以觸發角色自動指派)
        _testUsername = $"pv_{Guid.NewGuid().ToString()[..7]}_test_user";
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 1. 建立測試用戶
        _testUserId = Guid.NewGuid();
        var insertUserSql =
            @"
            INSERT INTO users (id, account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@id, @account, @password_hash, @display_name, 1, false, NOW(), NOW())
            ON CONFLICT (account) DO NOTHING;
        ";

        await using (var command = new NpgsqlCommand(insertUserSql, connection))
        {
            command.Parameters.AddWithValue("id", _testUserId);
            command.Parameters.AddWithValue("account", _testUsername);
            command.Parameters.AddWithValue(
                "password_hash",
                BCrypt.Net.BCrypt.HashPassword(_testPassword, 12)
            );
            command.Parameters.AddWithValue("display_name", "Permission Validation Tester");
            await command.ExecuteNonQueryAsync();
        }

        // 2. 建立測試角色
        _testRoleId1 = Guid.NewGuid();
        _testRoleId2 = Guid.NewGuid();
        var insertRoleSql =
            @"
            INSERT INTO roles (id, role_name, description, version, is_deleted, created_at, updated_at)
            VALUES (@id1, @name1, @desc1, 1, false, NOW(), NOW()),
                   (@id2, @name2, @desc2, 1, false, NOW(), NOW());
        ";

        await using (var command = new NpgsqlCommand(insertRoleSql, connection))
        {
            command.Parameters.AddWithValue("id1", _testRoleId1);
            command.Parameters.AddWithValue(
                "name1",
                $"Tester_Role_1_{Guid.NewGuid().ToString()[..8]}"
            );
            command.Parameters.AddWithValue("desc1", "First role for permission tests");
            command.Parameters.AddWithValue("id2", _testRoleId2);
            command.Parameters.AddWithValue(
                "name2",
                $"Tester_Role_2_{Guid.NewGuid().ToString()[..8]}"
            );
            command.Parameters.AddWithValue("desc2", "Second role for permission tests");
            await command.ExecuteNonQueryAsync();
        }

        // 3. 建立測試權限
        _testPermissionId1 = Guid.NewGuid();
        _testPermissionId2 = Guid.NewGuid();
        _testPermissionCode1 = $"permission.validation.test1.{Guid.NewGuid().ToString()[..8]}";
        _testPermissionCode2 = $"permission.validation.test2.{Guid.NewGuid().ToString()[..8]}";
        var insertPermSql =
            @"
            INSERT INTO permissions (id, permission_code, name, description, permission_type, version, is_deleted, created_at, updated_at)
            VALUES (@id1, @code1, @name1, @desc1, @type1, 1, false, NOW(), NOW()),
                   (@id2, @code2, @name2, @desc2, @type2, 1, false, NOW(), NOW());
        ";

        await using (var command = new NpgsqlCommand(insertPermSql, connection))
        {
            command.Parameters.AddWithValue("id1", _testPermissionId1);
            command.Parameters.AddWithValue("code1", _testPermissionCode1);
            command.Parameters.AddWithValue("name1", "Test Permission 1");
            command.Parameters.AddWithValue("desc1", "First test permission");
            command.Parameters.AddWithValue("type1", "function");
            command.Parameters.AddWithValue("id2", _testPermissionId2);
            command.Parameters.AddWithValue("code2", _testPermissionCode2);
            command.Parameters.AddWithValue("name2", "Test Permission 2");
            command.Parameters.AddWithValue("desc2", "Second test permission");
            command.Parameters.AddWithValue("type2", "function");
            await command.ExecuteNonQueryAsync();
        }

        // 4. 指派權限到角色
        var assignPermSql =
            @"
            INSERT INTO role_permissions (role_id, permission_id, assigned_at, assigned_by)
            VALUES (@roleId1, @permId1, NOW(), NULL),
                   (@roleId2, @permId2, NOW(), NULL);
        ";

        await using (var command = new NpgsqlCommand(assignPermSql, connection))
        {
            command.Parameters.AddWithValue("roleId1", _testRoleId1);
            command.Parameters.AddWithValue("permId1", _testPermissionId1);
            command.Parameters.AddWithValue("roleId2", _testRoleId2);
            command.Parameters.AddWithValue("permId2", _testPermissionId2);
            await command.ExecuteNonQueryAsync();
        }

        // 5. 指派角色到用戶（兩個角色）
        var assignRoleSql =
            @"
            INSERT INTO user_roles (user_id, role_id, assigned_at, assigned_by, is_deleted)
            VALUES (@userId, @roleId1, NOW(), NULL, false),
                   (@userId, @roleId2, NOW(), NULL, false);
        ";

        await using (var command = new NpgsqlCommand(assignRoleSql, connection))
        {
            command.Parameters.AddWithValue("userId", _testUserId);
            command.Parameters.AddWithValue("roleId1", _testRoleId1);
            command.Parameters.AddWithValue("roleId2", _testRoleId2);
            await command.ExecuteNonQueryAsync();
        }

        // 6. 登入取得 token
        var loginRequest = new LoginRequest { Account = _testUsername, Password = _testPassword };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (loginResponse.IsSuccessStatusCode)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(
                content,
                _jsonOptions
            );
            _testToken = apiResponse?.Data?.Token;

            if (!string.IsNullOrEmpty(_testToken))
            {
                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _testToken);
            }
        }
    }

    /// <summary>
    /// 清理測試數據
    /// </summary>
    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var cleanupSql =
            @"
            DELETE FROM audit_logs WHERE operator_id = @userId;
            DELETE FROM user_roles WHERE user_id = @userId;
            DELETE FROM role_permissions WHERE role_id IN (@roleId1, @roleId2);
            DELETE FROM permission_failure_logs WHERE user_id = @userId;
            DELETE FROM users WHERE id = @userId;
            DELETE FROM roles WHERE id IN (@roleId1, @roleId2);
            DELETE FROM permissions WHERE id IN (@permId1, @permId2);
        ";

        await using var command = new NpgsqlCommand(cleanupSql, connection);
        command.Parameters.AddWithValue("userId", _testUserId);
        command.Parameters.AddWithValue("roleId1", _testRoleId1);
        command.Parameters.AddWithValue("roleId2", _testRoleId2);
        command.Parameters.AddWithValue("permId1", _testPermissionId1);
        command.Parameters.AddWithValue("permId2", _testPermissionId2);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 測試：多角色權限合併
    /// 預期：用戶擁有兩個角色的所有權限
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_WithMultipleRoles_ReturnsUnionOfAllPermissions()
    {
        var response = await _client.GetAsync($"/api/users/{_testUserId}/roles/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<UserEffectivePermissionsDto>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(_testUserId);

        var permissionCodes = apiResponse.Data.Permissions.Select(p => p.PermissionCode).ToList();
        permissionCodes.Should().Contain(_testPermissionCode1);
        permissionCodes.Should().Contain(_testPermissionCode2);
    }

    /// <summary>
    /// 測試：未授權用戶無法訪問
    /// 預期：返回 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_WithoutToken_ReturnsUnauthorized()
    {
        var unauthorizedClient = _factory.CreateClient();

        var response = await unauthorizedClient.GetAsync(
            $"/api/users/{_testUserId}/roles/permissions"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 測試：不存在的用戶返回空權限
    /// 預期：返回成功，但權限列表為空
    /// </summary>
    [Fact]
    public async Task GetUserEffectivePermissions_UserDoesNotExist_ReturnsEmptyPermissions()
    {
        var nonExistentUserId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/users/{nonExistentUserId}/roles/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<UserEffectivePermissionsDto>>(
            content,
            _jsonOptions
        );

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Permissions.Should().BeEmpty();
    }
}
