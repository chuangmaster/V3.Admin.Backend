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
/// 用戶角色指派控制器集成測試
/// 測試用戶角色指派、移除、查詢功能
/// </summary>
[Collection("Integration")]
public class UserRoleControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private Guid _testUserId = Guid.Empty;
    private Guid _testRoleId1 = Guid.Empty;
    private Guid _testRoleId2 = Guid.Empty;
    private string? _testToken;
    private const string _testUsername = "user_role_test_user";
    private const string _testPassword = "TestPass@123";

    public UserRoleControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// 初始化測試環境：建立測試用戶、角色
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 1. 建立測試用戶
        var insertUserSql =
            @"
            INSERT INTO users (username, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@username, @password_hash, @display_name, 1, false, NOW(), NOW())
            RETURNING id;
        ";

        await using (var command = new NpgsqlCommand(insertUserSql, connection))
        {
            command.Parameters.AddWithValue("username", _testUsername);
            command.Parameters.AddWithValue(
                "password_hash",
                BCrypt.Net.BCrypt.HashPassword(_testPassword, 12)
            );
            command.Parameters.AddWithValue("display_name", "User Role Tester");
            var result = await command.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var userId))
            {
                _testUserId = userId;
            }
        }

        // 2. 建立測試角色 1
        var insertRole1Sql =
            @"
            INSERT INTO roles (role_name, description, is_deleted, version, created_at)
            VALUES (@role_name, @description, false, 1, NOW())
            RETURNING id;
        ";

        await using (var command = new NpgsqlCommand(insertRole1Sql, connection))
        {
            command.Parameters.AddWithValue(
                "role_name",
                "TestRole1_" + Guid.NewGuid().ToString().Substring(0, 8)
            );
            command.Parameters.AddWithValue("description", "Test Role 1");
            var result = await command.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var roleId))
            {
                _testRoleId1 = roleId;
            }
        }

        // 3. 建立測試角色 2
        var insertRole2Sql =
            @"
            INSERT INTO roles (role_name, description, is_deleted, version, created_at)
            VALUES (@role_name, @description, false, 1, NOW())
            RETURNING id;
        ";

        await using (var command = new NpgsqlCommand(insertRole2Sql, connection))
        {
            command.Parameters.AddWithValue(
                "role_name",
                "TestRole2_" + Guid.NewGuid().ToString().Substring(0, 8)
            );
            command.Parameters.AddWithValue("description", "Test Role 2");
            var result = await command.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var roleId))
            {
                _testRoleId2 = roleId;
            }
        }

        // 4. 登入獲取令牌
        var loginRequest = new LoginRequest { Username = _testUsername, Password = _testPassword };

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

    public async Task DisposeAsync()
    {
        // 清理測試資料
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        // 刪除用戶角色關聯
        if (_testUserId != Guid.Empty)
        {
            var deleteUserRolesSql = "DELETE FROM user_roles WHERE user_id = @user_id;";
            await using var command = new NpgsqlCommand(deleteUserRolesSql, connection);
            command.Parameters.AddWithValue("user_id", _testUserId);
            await command.ExecuteNonQueryAsync();
        }

        // 刪除用戶
        if (_testUserId != Guid.Empty)
        {
            var deleteUserSql = "DELETE FROM users WHERE id = @id;";
            await using var command = new NpgsqlCommand(deleteUserSql, connection);
            command.Parameters.AddWithValue("id", _testUserId);
            await command.ExecuteNonQueryAsync();
        }

        // 刪除角色
        if (_testRoleId1 != Guid.Empty)
        {
            var deleteRoleSql = "DELETE FROM roles WHERE id = @id;";
            await using var command = new NpgsqlCommand(deleteRoleSql, connection);
            command.Parameters.AddWithValue("id", _testRoleId1);
            await command.ExecuteNonQueryAsync();
        }

        if (_testRoleId2 != Guid.Empty)
        {
            var deleteRoleSql = "DELETE FROM roles WHERE id = @id;";
            await using var command = new NpgsqlCommand(deleteRoleSql, connection);
            command.Parameters.AddWithValue("id", _testRoleId2);
            await command.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task AssignUserRole_WithValidRoleId_ReturnsSuccess()
    {
        // Arrange
        var request = new AssignUserRoleRequest { RoleIds = new List<Guid> { _testRoleId1 } };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<object>>(
            content,
            _jsonOptions
        );
        apiResponse?.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AssignUserRole_WithDuplicateRole_ReturnsFail()
    {
        // Arrange - 先指派一個角色
        var request = new AssignUserRoleRequest { RoleIds = new List<Guid> { _testRoleId1 } };
        await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", request);

        // 再試著指派同一個角色
        // Act
        var response = await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserRoles_WithAssignedRoles_ReturnsRoleList()
    {
        // Arrange - 先指派角色
        var assignRequest = new AssignUserRoleRequest
        {
            RoleIds = new List<Guid> { _testRoleId1, _testRoleId2 },
        };
        await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", assignRequest);

        // Act
        var response = await _client.GetAsync($"/api/users/{_testUserId}/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<List<UserRoleDto>>>(
            content,
            _jsonOptions
        );

        apiResponse?.Success.Should().BeTrue();
        apiResponse?.Data?.Should().HaveCount(2);
        apiResponse?.Data?.Should().ContainSingle(r => r.RoleId == _testRoleId1);
        apiResponse?.Data?.Should().ContainSingle(r => r.RoleId == _testRoleId2);
    }

    [Fact]
    public async Task RemoveUserRole_WithAssignedRole_ReturnsSuccess()
    {
        // Arrange - 先指派角色
        var assignRequest = new AssignUserRoleRequest { RoleIds = new List<Guid> { _testRoleId1 } };
        await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", assignRequest);

        // Act
        var response = await _client.DeleteAsync($"/api/users/{_testUserId}/roles/{_testRoleId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveUserRole_VerifiesSoftDelete()
    {
        // Arrange
        var assignRequest = new AssignUserRoleRequest { RoleIds = new List<Guid> { _testRoleId1 } };
        await _client.PostAsJsonAsync($"/api/users/{_testUserId}/roles", assignRequest);

        // Act
        await _client.DeleteAsync($"/api/users/{_testUserId}/roles/{_testRoleId1}");

        // 查詢用戶角色
        var getResponse = await _client.GetAsync($"/api/users/{_testUserId}/roles");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<List<UserRoleDto>>>(
            content,
            _jsonOptions
        );

        // 軟刪除後，已刪除的角色應該不在列表中
        apiResponse?.Data?.Should().NotContain(r => r.RoleId == _testRoleId1);
    }

    [Fact]
    public async Task GetUserRoles_WithoutAuthToken_ReturnsUnauthorized()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync($"/api/users/{_testUserId}/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
