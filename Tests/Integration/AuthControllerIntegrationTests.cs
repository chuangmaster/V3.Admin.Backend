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
public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        // 在每個測試前插入測試用的管理員帳號
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var insertSql = @"
            INSERT INTO users (account, password_hash, display_name, version, is_deleted, created_at, updated_at)
            VALUES (@account, @password_hash, @display_name, 1, false, NOW(), NOW())
            ON CONFLICT (account) DO NOTHING;
        ";

        await using var command = new Npgsql.NpgsqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("account", "admin");
        command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword("Admin@123", 12));
        command.Parameters.AddWithValue("display_name", "管理員");
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        // 清理測試數據
        await using var connection = new Npgsql.NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var deleteSql = "DELETE FROM users WHERE account = 'admin';";
        await using var command = new Npgsql.NpgsqlCommand(deleteSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = "admin",
            Password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Code.Should().Be(ResponseCodes.SUCCESS);
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
        apiResponse.Data.User.Account.Should().Be("admin");
        apiResponse.Data.User.DisplayName.Should().Be("管理員");
    }

    [Fact]
    public async Task Login_WithInvalidAccount_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = "nonexistent",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.INVALID_CREDENTIALS);
        apiResponse.Message.Should().Contain("帳號或密碼錯誤");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = "admin",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.INVALID_CREDENTIALS);
    }

    [Fact]
    public async Task Login_WithInvalidRequest_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = "ad", // 太短,但會因為帳號不存在而返回 401
            Password = "123"  // 太短
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(content, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.INVALID_CREDENTIALS);
        apiResponse.Message.Should().Contain("帳號或密碼錯誤");
    }
}
