using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration;

[Collection("Integration")]
public class PermissionControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private Guid _testPermissionId = Guid.Empty;
    private string? _testToken;

    public PermissionControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        // Setup: Create test user and get token
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var insertUserSql = @"
            INSERT INTO users (username, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@username, @password_hash, @display_name, 1, false, NOW(), NOW())
            ON CONFLICT (username) DO NOTHING;
        ";

        await using var command = new Npgsql.NpgsqlCommand(insertUserSql, connection);
        command.Parameters.AddWithValue("username", "permission_test_user");
        command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword("TestPass@123", 12));
        command.Parameters.AddWithValue("display_name", "Permission Tester");
        await command.ExecuteNonQueryAsync();

        // Login to get token
        var loginRequest = new LoginRequest
        {
            Username = "permission_test_user",
            Password = "TestPass@123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (loginResponse.IsSuccessStatusCode)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(content, _jsonOptions);
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
        // Cleanup: Delete test permission and user
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        if (_testPermissionId != Guid.Empty)
        {
            var deletePermSql = "DELETE FROM permissions WHERE id = @id;";
            await using var permCommand = new Npgsql.NpgsqlCommand(deletePermSql, connection);
            permCommand.Parameters.AddWithValue("id", _testPermissionId);
            await permCommand.ExecuteNonQueryAsync();
        }

        var deleteUserSql = "DELETE FROM users WHERE username = 'permission_test_user';";
        await using var command = new Npgsql.NpgsqlCommand(deleteUserSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GetPermissions_WithoutAuthToken_ReturnsUnauthorized()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync("/api/permission");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissions_WithValidToken_ReturnsSuccessWithEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/permission?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<PermissionListResponse>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePermission_WithValidRequest_ReturnsCreatedWithPermission()
    {
        // Arrange
        var request = new CreatePermissionRequest
        {
            PermissionCode = "test.permission.read",
            Name = "Test Permission Read",
            Description = "Test permission for reading",
            PermissionType = "function"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/permission", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<PermissionResponse>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.PermissionCode.Should().Be("test.permission.read");

        _testPermissionId = apiResponse.Data.Id;
    }

    [Fact]
    public async Task CreatePermission_WithDuplicateCode_ReturnsBadRequest()
    {
        // Arrange
        var firstRequest = new CreatePermissionRequest
        {
            PermissionCode = "duplicate.test.perm",
            Name = "First Test",
            Description = "First",
            PermissionType = "function"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/permission", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var firstApiResponse = JsonSerializer.Deserialize<PermissionResponse>(firstContent, _jsonOptions);
        _testPermissionId = firstApiResponse!.Data!.Id;

        // Arrange second request with duplicate code
        var secondRequest = new CreatePermissionRequest
        {
            PermissionCode = "duplicate.test.perm",
            Name = "Second Test",
            Description = "Second",
            PermissionType = "function"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/permission", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.DUPLICATE_PERMISSION_CODE);
    }

    [Fact]
    public async Task GetPermission_WithValidId_ReturnsSuccessWithPermission()
    {
        // Arrange - Create a test permission
        var createRequest = new CreatePermissionRequest
        {
            PermissionCode = "get.test.permission",
            Name = "Get Test Permission",
            Description = "For getting",
            PermissionType = "function"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/permission", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<PermissionResponse>(createContent, _jsonOptions);
        var permissionId = createApiResponse!.Data!.Id;
        _testPermissionId = permissionId;

        // Act
        var response = await _client.GetAsync($"/api/permission/{permissionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<PermissionResponse>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(permissionId);
        apiResponse.Data.PermissionCode.Should().Be("get.test.permission");
    }

    [Fact]
    public async Task GetPermission_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/permission/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.PERMISSION_NOT_FOUND);
    }

    [Fact]
    public async Task UpdatePermission_WithValidRequest_ReturnsSuccessWithUpdatedPermission()
    {
        // Arrange - Create a permission
        var createRequest = new CreatePermissionRequest
        {
            PermissionCode = "update.test.perm",
            Name = "Original Name",
            Description = "Original Description",
            PermissionType = "function"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/permission", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<PermissionResponse>(createContent, _jsonOptions);
        var permissionId = createApiResponse!.Data!.Id;
        var version = createApiResponse.Data.Version;
        _testPermissionId = permissionId;

        // Arrange update request
        var updateRequest = new UpdatePermissionRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Version = version
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/permission/{permissionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<PermissionResponse>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("Updated Name");
        apiResponse.Data.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdatePermission_WithVersionMismatch_ReturnsConflict()
    {
        // Arrange - Create a permission
        var createRequest = new CreatePermissionRequest
        {
            PermissionCode = "version.conflict.test",
            Name = "Version Conflict Test",
            Description = "Test",
            PermissionType = "function"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/permission", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<PermissionResponse>(createContent, _jsonOptions);
        var permissionId = createApiResponse!.Data!.Id;
        _testPermissionId = permissionId;

        // Arrange update with wrong version
        var updateRequest = new UpdatePermissionRequest
        {
            Name = "Updated",
            Description = "Updated",
            Version = 999  // Wrong version
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/permission/{permissionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
    }

    [Fact]
    public async Task DeletePermission_WithValidRequest_ReturnsSuccess()
    {
        // Arrange - Create a permission
        var createRequest = new CreatePermissionRequest
        {
            PermissionCode = "delete.test.perm",
            Name = "Delete Test",
            Description = "To be deleted",
            PermissionType = "function"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/permission", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<PermissionResponse>(createContent, _jsonOptions);
        var permissionId = createApiResponse!.Data!.Id;
        var version = createApiResponse.Data.Version;

        // Arrange delete request
        var deleteRequest = new DeletePermissionRequest
        {
            Version = version
        };

        // Act
        var response = await _client.DeleteAsync($"/api/permission/{permissionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Code.Should().Be(ResponseCodes.SUCCESS);
    }
}
