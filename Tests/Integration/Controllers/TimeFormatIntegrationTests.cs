using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Requests;
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

    public TimeFormatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
    public async Task QueryAuditLog_WithNonUtc0Timezone_ReturnsBadRequest()
    {
        // Arrange - 手動構造 JSON 以繞過客戶端序列化
        var json = """
        {
            "startTime": "2026-01-01T00:00:00+08:00",
            "endTime": "2026-01-31T23:59:59+08:00"
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
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);
        apiResponse.Errors.Should().NotBeNull();
        apiResponse.Errors!["detail"].ToString().Should().Contain("UTC0");
    }

    [Fact]
    public async Task QueryAuditLog_WithInvalidDateFormat_ReturnsBadRequest()
    {
        // Arrange - 無效的日期格式
        var json = """
        {
            "startTime": "2026/01/01 00:00:00",
            "endTime": "2026/01/31 23:59:59"
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
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);
    }

    #endregion

    #region QueryServiceOrdersRequest Tests

    [Fact]
    public async Task QueryServiceOrders_WithValidUtc0Format_ReturnsSuccess()
    {
        // Arrange - 使用 GET 請求查詢參數
        var startDate = DateTimeOffset.Parse("2026-01-01T00:00:00.000Z");
        var endDate = DateTimeOffset.Parse("2026-01-31T23:59:59.999Z");

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={startDate:O}&serviceDateEnd={endDate:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QueryServiceOrders_WithNonUtc0Timezone_ReturnsBadRequest()
    {
        // Arrange - 使用非 UTC0 時區 (使用 GET 請求)
        var startDate = "2026-01-01T00:00:00+09:00";
        var endDate = "2026-01-31T23:59:59+09:00";

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={Uri.EscapeDataString(startDate)}&serviceDateEnd={Uri.EscapeDataString(endDate)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task QueryServiceOrders_WithMissingTimezone_ReturnsBadRequest()
    {
        // Arrange - 缺少時區識別符 Z
        var startDate = "2026-01-01T00:00:00";
        var endDate = "2026-01-31T23:59:59";

        // Act
        var response = await _client.GetAsync($"/api/service-orders?serviceDateStart={Uri.EscapeDataString(startDate)}&serviceDateEnd={Uri.EscapeDataString(endDate)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
    public async Task CreateConsignmentOrder_WithNonUtc0Timezone_ReturnsBadRequest()
    {
        // Arrange - 使用非 UTC0 時區
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);
        apiResponse.Errors.Should().NotBeNull();
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseModel>(responseContent, _jsonOptions);

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);

        // 應該是 FluentValidation 的業務規則錯誤,而非 JsonException
        apiResponse.Errors.Should().NotBeNull();
        apiResponse.Errors.Should().NotContainKey("detail"); // JsonException 才有 detail
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task InvalidTimeFormat_ReturnsStandardizedErrorResponse()
    {
        // Arrange
        var json = """
        {
            "startTime": "invalid-date-format"
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
        apiResponse.Code.Should().Be(ResponseCodes.VALIDATION_ERROR);
        apiResponse.Message.Should().NotBeNullOrEmpty();
        apiResponse.TraceId.Should().NotBeNullOrEmpty();
        apiResponse.Errors.Should().NotBeNull();
    }

    #endregion
}
