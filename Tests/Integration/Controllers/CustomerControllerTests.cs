using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration.Controllers;

/// <summary>
/// CustomerController 整合測試 (US1)
/// </summary>
[Collection("Integration")]
public class CustomerControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    private Guid _testUserId = Guid.Empty;
    private string? _testToken;
    private Guid _createdCustomerId = Guid.Empty;

    private const string TestUsername = "cust_ctrl_test_user";
    private const string TestPassword = "TestPass@123";

    public CustomerControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        const string insertUserSql = @"
            INSERT INTO users (username, password_hash, display_name, version, is_deleted, created_at)
            VALUES (@username, @password_hash, @display_name, 1, false, NOW())
            RETURNING id;
        ";

        await using (var command = new NpgsqlCommand(insertUserSql, connection))
        {
            command.Parameters.AddWithValue("username", TestUsername);
            command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword(TestPassword, 12));
            command.Parameters.AddWithValue("display_name", "Customer Controller Tester");

            var result = await command.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var userId))
            {
                _testUserId = userId;
            }
        }

        var loginRequest = new LoginRequest { Username = TestUsername, Password = TestPassword };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(loginJson, _jsonOptions);
        _testToken = apiResponse?.Data?.Token;
        _testToken.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _testToken);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        if (_createdCustomerId != Guid.Empty)
        {
            const string deleteCustomerSql = "DELETE FROM customers WHERE id = @id;";
            await using var cmd = new NpgsqlCommand(deleteCustomerSql, connection);
            cmd.Parameters.AddWithValue("id", _createdCustomerId);
            await cmd.ExecuteNonQueryAsync();
        }

        if (_testUserId != Guid.Empty)
        {
            const string deleteAuditLogsSql = "DELETE FROM audit_logs WHERE operator_id = @id;";
            await using (var cmd = new NpgsqlCommand(deleteAuditLogsSql, connection))
            {
                cmd.Parameters.AddWithValue("id", _testUserId);
                await cmd.ExecuteNonQueryAsync();
            }

            const string deleteUserRolesSql = "DELETE FROM user_roles WHERE user_id = @user_id;";
            await using (var cmd = new NpgsqlCommand(deleteUserRolesSql, connection))
            {
                cmd.Parameters.AddWithValue("user_id", _testUserId);
                await cmd.ExecuteNonQueryAsync();
            }

            const string deleteUserSql = "DELETE FROM users WHERE id = @id;";
            await using (var cmd = new NpgsqlCommand(deleteUserSql, connection))
            {
                cmd.Parameters.AddWithValue("id", _testUserId);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    [Fact]
    public async Task CreateCustomer_ThenSearch_ReturnsCreatedCustomer()
    {
        var createRequest = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = "0912-345678",
            Email = "ming@example.com",
            IdNumber = "A123456789",
        };

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ApiResponseModel<CustomerResponse>>(createJson, _jsonOptions);
        created?.Success.Should().BeTrue();
        created?.Data.Should().NotBeNull();
        created!.Data!.Id.Should().NotBe(Guid.Empty);
        created.Data.IdNumber.Should().Be(createRequest.IdNumber);

        _createdCustomerId = created.Data.Id;

        var searchResponse = await _client.GetAsync(
            "/api/customers/search?pageNumber=1&pageSize=20&IdNumber=A123456789"
        );

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchJson = await searchResponse.Content.ReadAsStringAsync();
        var searched = JsonSerializer.Deserialize<PagedApiResponseModel<CustomerResponse>>(searchJson, _jsonOptions);

        searched?.Success.Should().BeTrue();
        searched?.Data.Should().ContainSingle(x => x.Id == _createdCustomerId);
    }
}
