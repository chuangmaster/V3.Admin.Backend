using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Npgsql;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration;

/// <summary>
/// 服務單序號並發測試 (US1)
/// </summary>
[Collection("Integration")]
public class ServiceOrderConcurrencyTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string _testUsername = "so_conc_test_user";
    private const string _testPassword = "TestPass@123";

    private const string _png1x1Base64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMB/6X9c0cAAAAASUVORK5CYII=";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    private Guid _testUserId = Guid.Empty;

    public ServiceOrderConcurrencyTests(CustomWebApplicationFactory factory)
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
            command.Parameters.AddWithValue("username", _testUsername);
            command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword(_testPassword, 12));
            command.Parameters.AddWithValue("display_name", "Service Order Concurrency Tester");

            var result = await command.ExecuteScalarAsync();
            if (result != null && Guid.TryParse(result.ToString(), out var userId))
            {
                _testUserId = userId;
            }
        }

        var loginRequest = new LoginRequest { Username = _testUsername, Password = _testPassword };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel<LoginResponse>>(loginJson, _jsonOptions);
        apiResponse?.Data?.Token.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiResponse!.Data!.Token);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

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
    public async Task CreateBuybackOrders_100Concurrent_ProducesUniqueOrderNumbers()
    {
        // 先建立一個共用客戶
        var createCustomerRequest = new CreateCustomerRequest
        {
            Name = "林併發",
            PhoneNumber = "0912-345680",
            Email = "concurrency@example.com",
            IdNumber = "C123456789",
        };

        var customerResponse = await _client.PostAsJsonAsync("/api/customers", createCustomerRequest);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var customerJson = await customerResponse.Content.ReadAsStringAsync();
        var customerApi = JsonSerializer.Deserialize<ApiResponseModel<CustomerResponse>>(customerJson, _jsonOptions);
        customerApi?.Success.Should().BeTrue();
        var customerId = customerApi!.Data!.Id;

        var tasks = Enumerable.Range(1, 100)
            .Select(async i =>
            {
                var request = new CreateBuybackOrderRequest
                {
                    OrderType = "BUYBACK",
                    OrderSource = "OFFLINE",
                    CustomerId = customerId,
                    TotalAmount = 100 + i,
                    ProductItems =
                    [
                        new CreateBuybackProductItemRequest
                        {
                            SequenceNumber = 1,
                            BrandName = "Brand",
                            StyleName = "Style",
                            InternalCode = $"CC{i:000}",
                        },
                    ],
                    IdCardImageBase64 = _png1x1Base64,
                    IdCardImageContentType = "image/png",
                    IdCardImageFileName = "id-card.png",
                };

                var resp = await _client.PostAsJsonAsync("/api/service-orders", request);
                resp.StatusCode.Should().Be(HttpStatusCode.Created);

                var json = await resp.Content.ReadAsStringAsync();
                var api = JsonSerializer.Deserialize<ApiResponseModel<ServiceOrderResponse>>(json, _jsonOptions);
                api?.Success.Should().BeTrue();
                api!.Data!.OrderNumber.Should().NotBeNullOrWhiteSpace();

                return api.Data.OrderNumber;
            })
            .ToList();

        string[] orderNumbers = await Task.WhenAll(tasks);

        orderNumbers.Should().HaveCount(100);
        orderNumbers.Distinct().Should().HaveCount(100);
    }
}
