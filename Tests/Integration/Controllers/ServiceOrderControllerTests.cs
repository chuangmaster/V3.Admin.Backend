using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Integration.Controllers;

/// <summary>
/// ServiceOrderController 整合測試 (US1)
/// </summary>
[Collection("Integration")]
public class ServiceOrderControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string _testUsername = "so_ctrl_test_user";
    private const string _testPassword = "TestPass@123";

    private const string _png1x1Base64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMB/6X9c0cAAAAASUVORK5CYII=";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    private Guid _testUserId = Guid.Empty;
    private string? _testToken;

    public ServiceOrderControllerTests(CustomWebApplicationFactory factory)
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
            command.Parameters.AddWithValue("display_name", "Service Order Controller Tester");

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
        _testToken = apiResponse?.Data?.Token;
        _testToken.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _testToken);
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
    public async Task CreateBuybackOrder_Preview_Merge_Confirm_Flow_Works()
    {
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            NewCustomer = new CreateCustomerRequest
            {
                Name = "陳大華",
                PhoneNumber = "0912-345679",
                Email = "dahua@example.com",
                IdNumber = "B123456789",
            },
            TotalAmount = 12345,
            ProductItems =
            [
                new CreateBuybackProductItemRequest
                {
                    SequenceNumber = 1,
                    BrandName = "LV",
                    StyleName = "Speedy",
                    InternalCode = "X001",
                },
            ],
            IdCardImageBase64 = _png1x1Base64,
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id-card.png",
        };

        var previewResponse = await _client.PostAsJsonAsync(
            "/api/service-orders/buyback/contract/preview",
            request
        );
        var previewDebugBody = await previewResponse.Content.ReadAsStringAsync();
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK, previewDebugBody);

        var previewApi = JsonSerializer.Deserialize<ApiResponseModel<PdfBase64Response>>(previewDebugBody, _jsonOptions);
        previewApi?.Success.Should().BeTrue();
        previewApi?.Data?.PdfBase64.Should().NotBeNullOrWhiteSpace();

        var createResponse = await _client.PostAsJsonAsync("/api/service-orders", request);
        var createDebugBody = await createResponse.Content.ReadAsStringAsync();

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            string? serviceExceptionMessage = await TryCreateOrderByServiceForDebugAsync(request);
            createResponse.StatusCode.Should().Be(
                HttpStatusCode.Created,
                $"{createDebugBody} | ServiceException: {serviceExceptionMessage}"
            );
        }

        var createApi = JsonSerializer.Deserialize<ApiResponseModel<ServiceOrderResponse>>(createDebugBody, _jsonOptions);
        createApi?.Success.Should().BeTrue();
        createApi?.Data.Should().NotBeNull();

        var createdOrder = createApi!.Data!;
        createdOrder.Id.Should().NotBe(Guid.Empty);
        createdOrder.OrderNumber.Should().StartWith("BS");
        createdOrder.Status.Should().Be("PENDING");
        createdOrder.Attachments.Should().ContainSingle(a => a.AttachmentType == "ID_CARD");

        var mergeRequest = new MergeSignatureRequest
        {
            PdfBase64 = previewApi!.Data!.PdfBase64,
            SignatureBase64Png = _png1x1Base64,
            PageIndex = 0,
            X = 40,
            Y = 200,
            Width = 120,
            Height = 40,
        };

        var mergeResponse = await _client.PostAsJsonAsync(
            $"/api/service-orders/{createdOrder.Id}/signatures/merge-preview",
            mergeRequest
        );
        mergeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mergeJson = await mergeResponse.Content.ReadAsStringAsync();
        var mergeApi = JsonSerializer.Deserialize<ApiResponseModel<PdfBase64Response>>(mergeJson, _jsonOptions);
        mergeApi?.Success.Should().BeTrue();
        mergeApi?.Data?.PdfBase64.Should().NotBeNullOrWhiteSpace();

        var confirmRequest = new ConfirmOrderRequest
        {
            DocumentType = "BUYBACK_CONTRACT",
            PdfBase64 = mergeApi!.Data!.PdfBase64,
            SignatureBase64Png = _png1x1Base64,
            SignerName = "陳大華",
            FileName = "BUYBACK_CONTRACT.pdf",
        };

        var confirmResponse = await _client.PostAsJsonAsync(
            $"/api/service-orders/{createdOrder.Id}/signatures/offline",
            confirmRequest
        );

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmJson = await confirmResponse.Content.ReadAsStringAsync();
        var confirmApi = JsonSerializer.Deserialize<ApiResponseModel<ConfirmOrderResponse>>(confirmJson, _jsonOptions);
        confirmApi?.Success.Should().BeTrue();
        confirmApi?.Data.Should().NotBeNull();
        confirmApi!.Data!.AttachmentId.Should().NotBe(Guid.Empty);
        confirmApi.Data.SignatureRecordId.Should().NotBe(Guid.Empty);
        confirmApi.Data.BlobPath.Should().NotBeNullOrWhiteSpace();
        confirmApi.Data.SasUrl.Should().StartWith("https://fake-blob.local/");

        await using var conn = new NpgsqlConnection(_factory.ConnectionString);
        await conn.OpenAsync();

        const string statusSql = "SELECT status FROM service_orders WHERE id = @id;";
        await using var statusCmd = new NpgsqlCommand(statusSql, conn);
        statusCmd.Parameters.AddWithValue("id", createdOrder.Id);
        var status = await statusCmd.ExecuteScalarAsync();

        status?.ToString().Should().Be("COMPLETED");
    }

    private async Task<string?> TryCreateOrderByServiceForDebugAsync(CreateBuybackOrderRequest request)
    {
        try
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IServiceOrderService>();

            // 使用不同 IdNumber 避免與前一次 API 呼叫造成資料重複。
            var requestForDebug = new CreateBuybackOrderRequest
            {
                OrderType = request.OrderType,
                OrderSource = request.OrderSource,
                CustomerId = request.CustomerId,
                NewCustomer = request.NewCustomer is null
                    ? null
                    : new CreateCustomerRequest
                    {
                        Name = request.NewCustomer.Name,
                        PhoneNumber = request.NewCustomer.PhoneNumber,
                        Email = request.NewCustomer.Email,
                        IdNumber = $"{request.NewCustomer.IdNumber[..^1]}0",
                    },
                TotalAmount = request.TotalAmount,
                ProductItems = request.ProductItems,
                IdCardImageBase64 = request.IdCardImageBase64,
                IdCardImageContentType = request.IdCardImageContentType,
                IdCardImageFileName = request.IdCardImageFileName,
            };

            _ = await service.CreateBuybackOrderAsync(requestForDebug, _testUserId, CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}
