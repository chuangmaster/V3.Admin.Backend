# UTC0 時間標準化 - 快速開始指南

## 概述

本指南說明如何在 V3 Admin Backend API 中正確處理時間資料。系統採用 **UTC0 時間標準化**,確保所有時間資料的一致性和跨時區的正確性。

## 核心原則

1. **前端到後端**: 所有時間必須轉換為 UTC0 格式再傳送
2. **後端到前端**: 所有時間回應維持 UTC0 格式
3. **資料庫儲存**: 所有時間資料以 UTC0 格式儲存
4. **時區轉換**: 由前端負責,後端不處理

## 時間格式規範

### 標準格式

```
YYYY-MM-DDTHH:mm:ss.fffZ
```

- **YYYY**: 四位數年份
- **MM**: 兩位數月份 (01-12)
- **DD**: 兩位數日期 (01-31)
- **T**: 日期與時間的分隔符
- **HH**: 24 小時制小時 (00-23)
- **mm**: 分鐘 (00-59)
- **ss**: 秒 (00-59)
- **fff**: 毫秒 (000-999) - 三位數精度
- **Z**: UTC 時區識別符 (必須存在)

### 正確範例

```json
{
  "serviceDate": "2026-01-24T06:30:15.123Z",
  "createdAt": "2026-01-24T00:00:00.000Z",
  "updatedAt": "2026-01-24T12:45:30.999Z"
}
```

### 錯誤範例 ❌

```json
{
  "serviceDate": "2026-01-24T14:30:15+08:00",  // ❌ 包含時區偏移
  "createdAt": "2026-01-24T00:00:00",         // ❌ 缺少 Z 識別符和毫秒
  "updatedAt": "2026/01/24 12:45:30"          // ❌ 格式錯誤
}
```

## API 請求範例

### POST 請求 - 建立寄賣單

```http
POST /api/service-orders/consignment
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "serviceDate": "2026-02-01T10:00:00.000Z",
  "consignmentStartDate": "2026-02-01T00:00:00.000Z",
  "consignmentEndDate": "2026-02-28T23:59:59.999Z"
}
```

### GET 請求 - 查詢稽核日誌

```http
GET /api/auditlog/query
Authorization: Bearer {token}
Content-Type: application/json

{
  "startTime": "2026-01-01T00:00:00.000Z",
  "endTime": "2026-01-31T23:59:59.999Z",
  "pageNumber": 1,
  "pageSize": 20
}
```

## C# 程式碼範例

### Request Model 定義

```csharp
using System.ComponentModel.DataAnnotations;

namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 建立寄賣單請求
/// </summary>
public class CreateConsignmentOrderRequest
{
    /// <summary>
    /// 服務日期
    /// </summary>
    /// <example>2026-02-01T10:00:00.000Z</example>
    [Required(ErrorMessage = "服務日期為必填")]
    public DateTimeOffset ServiceDate { get; set; }

    /// <summary>
    /// 寄賣開始日期 (UTC0 格式)
    /// </summary>
    /// <example>2026-02-01T00:00:00.000Z</example>
    [Required(ErrorMessage = "寄賣開始日期為必填")]
    public DateTimeOffset ConsignmentStartDate { get; set; }

    /// <summary>
    /// 寄賣結束日期 (UTC0 格式)
    /// </summary>
    /// <example>2026-02-28T23:59:59.999Z</example>
    [Required(ErrorMessage = "寄賣結束日期為必填")]
    public DateTimeOffset ConsignmentEndDate { get; set; }

    // ... 其他欄位
}
```

### Response Model 定義

```csharp
namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 服務單回應
/// </summary>
public class ServiceOrderResponse
{
    /// <summary>
    /// 服務日期 (UTC0 格式)
    /// </summary>
    /// <example>2026-02-01T10:00:00.000Z</example>
    public DateTimeOffset ServiceDate { get; set; }

    /// <summary>
    /// 建立時間 (UTC0 格式)
    /// </summary>
    /// <example>2026-01-24T06:30:15.123Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新時間 (UTC0 格式)
    /// </summary>
    /// <example>2026-01-24T08:45:30.456Z</example>
    public DateTimeOffset UpdatedAt { get; set; }

    // ... 其他欄位
}
```

### Service 層時間處理

```csharp
using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Services;

public class ServiceOrderService : IServiceOrderService
{
    public async Task<ServiceOrderDto> CreateConsignmentOrderAsync(
        CreateConsignmentOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        // ✅ 使用 DateTimeOffset.UtcNow 獲取當前 UTC 時間
        var now = DateTimeOffset.UtcNow;

        // ✅ Request 中的時間已經是 UTC0 格式,直接使用
        var dto = new ServiceOrderDto
        {
            ServiceDate = request.ServiceDate,
            ConsignmentStartDate = request.ConsignmentStartDate,
            ConsignmentEndDate = request.ConsignmentEndDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        // 儲存到資料庫...
        await _repository.CreateAsync(dto, cancellationToken);

        return dto;
    }
}
```

### Repository 層時間處理

```csharp
using Dapper;
using System.Data;

namespace V3.Admin.Backend.Repositories;

public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly IDbConnection _connection;

    public async Task<int> CreateAsync(ServiceOrderDto dto, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO service_orders (
                service_date,
                consignment_start_date,
                consignment_end_date,
                created_at,
                updated_at
            )
            VALUES (
                @ServiceDate,
                @ConsignmentStartDate,
                @ConsignmentEndDate,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING id;
        ";

        // ✅ DateTimeOffset 會自動轉換為 PostgreSQL 的 timestamptz (UTC)
        var id = await _connection.QuerySingleAsync<int>(sql, dto);

        return id;
    }

    public async Task<ServiceOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                id,
                service_date AS ServiceDate,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM service_orders
            WHERE id = @Id
        ";

        // ✅ Dapper 會自動將 timestamptz 轉換為 DateTimeOffset (UTC)
        var dto = await _connection.QuerySingleOrDefaultAsync<ServiceOrderDto>(sql, new { Id = id });

        return dto;
    }
}
```

## 驗證與錯誤處理

### FluentValidation 驗證器

```csharp
using FluentValidation;
using V3.Admin.Backend.Validators;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 建立寄賣單請求驗證器
/// </summary>
public class CreateConsignmentOrderRequestValidator : AbstractValidator<CreateConsignmentOrderRequest>
{
    public CreateConsignmentOrderRequestValidator()
    {
        // 服務日期驗證
        RuleFor(x => x.ServiceDate)
            .NotEmpty().WithMessage("服務日期為必填")
            .SetValidator(new Utc0DateTimeValidator()); // ✅ 驗證 UTC0 格式

        // 寄賣開始日期驗證
        RuleFor(x => x.ConsignmentStartDate)
            .NotEmpty().WithMessage("寄賣開始日期為必填")
            .SetValidator(new Utc0DateTimeValidator())
            .LessThanOrEqualTo(x => x.ConsignmentEndDate)
            .WithMessage("寄賣開始日期不能晚於結束日期");

        // 寄賣結束日期驗證
        RuleFor(x => x.ConsignmentEndDate)
            .NotEmpty().WithMessage("寄賣結束日期為必填")
            .SetValidator(new Utc0DateTimeValidator());
    }
}
```

### 錯誤回應範例

當時間格式不正確時,API 會回傳標準化錯誤:

```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "請求驗證失敗",
  "data": null,
  "errors": {
    "ServiceDate": [
      "時間必須為 UTC0 格式 (以 Z 結尾): YYYY-MM-DDTHH:mm:ss.fffZ"
    ]
  },
  "traceId": "00-1234567890abcdef1234567890abcdef-1234567890abcdef-00",
  "timestamp": "2026-01-24T06:30:15.123Z"
}
```

## 前端整合指引

### JavaScript/TypeScript

```typescript
// ✅ 正確: 使用 toISOString() 轉換為 UTC0 格式
const serviceDate = new Date('2026-02-01 18:00'); // 台北時間 18:00
const request = {
  serviceDate: serviceDate.toISOString(), // "2026-02-01T10:00:00.000Z"
  // ... 其他欄位
};

// ✅ 正確: 解析回應中的時間
const response = await api.getServiceOrder(orderId);
const localTime = new Date(response.serviceDate); // 自動轉換為本地時區
console.log(localTime.toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }));
// 輸出: "2026/2/1 下午6:00:00"
```

### C# 前端 (Blazor/MAUI)

```csharp
// ✅ 正確: 建立 UTC 時間
var serviceDate = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);

// ✅ 正確: 從本地時間轉換為 UTC
var localTime = new DateTime(2026, 2, 1, 18, 0, 0, DateTimeKind.Local);
var utcTime = new DateTimeOffset(localTime.ToUniversalTime());

// ✅ 正確: 顯示為本地時間
var response = await api.GetServiceOrderAsync(orderId);
var localDisplay = response.ServiceDate.ToLocalTime();
Console.WriteLine(localDisplay.ToString("yyyy/MM/dd HH:mm:ss"));
```

## 資料庫配置

### PostgreSQL 連線字串

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=v3admin;Username=postgres;Password=***;Timezone=UTC"
  }
}
```

**重要**: 連線字串必須包含 `Timezone=UTC` 參數,確保所有資料庫操作都使用 UTC 時區。

### 資料表定義範例

```sql
CREATE TABLE service_orders (
    id SERIAL PRIMARY KEY,
    service_date TIMESTAMPTZ NOT NULL,
    consignment_start_date TIMESTAMPTZ NOT NULL,
    consignment_end_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

-- 建議: 為時間欄位建立索引以提升查詢效能
CREATE INDEX idx_service_orders_service_date ON service_orders(service_date);
CREATE INDEX idx_service_orders_created_at ON service_orders(created_at);
```

## 測試範例

### 單元測試

```csharp
using Xunit;
using FluentAssertions;

public class Utc0DateTimeValidatorTests
{
    private readonly Utc0DateTimeValidator _validator = new();

    [Fact]
    public void Validate_WithUtc0Format_ReturnsValid()
    {
        // Arrange
        var dateTime = DateTimeOffset.Parse("2026-01-24T06:00:00.000Z");

        // Act
        var result = _validator.IsValid(null!, dateTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNonUtcOffset_ReturnsInvalid()
    {
        // Arrange
        var dateTime = DateTimeOffset.Parse("2026-01-24T14:00:00+08:00");

        // Act
        var result = _validator.IsValid(null!, dateTime);

        // Assert
        result.Should().BeFalse();
    }
}
```

### 整合測試

```csharp
using Xunit;
using FluentAssertions;
using System.Net.Http.Json;

public class ServiceOrderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ServiceOrderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateConsignmentOrder_WithUtc0Times_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateConsignmentOrderRequest
        {
            ServiceDate = DateTimeOffset.Parse("2026-02-01T10:00:00.000Z"),
            ConsignmentStartDate = DateTimeOffset.Parse("2026-02-01T00:00:00.000Z"),
            ConsignmentEndDate = DateTimeOffset.Parse("2026-02-28T23:59:59.999Z"),
            CustomerId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/service-orders/consignment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<ServiceOrderResponse>>();
        result.Data.ServiceDate.Offset.Should().Be(TimeSpan.Zero); // 確認是 UTC0
    }
}
```

## 常見問題 (FAQ)

### Q1: 為什麼要使用 DateTimeOffset 而不是 DateTime?

**A**: `DateTimeOffset` 明確包含時區資訊,可以避免時區混淆。`DateTime` 的 `Kind` 屬性容易被忽略,導致時區問題。

### Q2: 如果前端送來非 UTC0 的時間,會發生什麼?

**A**: API 會拒絕請求並回傳 400 Bad Request,錯誤訊息會明確說明需要 UTC0 格式。

### Q3: 資料庫中的舊資料怎麼處理?

**A**: 執行資料遷移腳本 (見 `Database/Migrations/009_MigrateToUtc0.sql`),將所有時間資料轉換為 UTC0 格式。

### Q4: 如何在 Swagger UI 中測試?

**A**: Swagger UI 會顯示時間欄位的格式範例 (例如: `2026-01-24T06:00:00.000Z`),直接使用這個格式填入測試資料即可。

### Q5: 系統日誌也使用 UTC0 嗎?

**A**: 是的,Serilog 已配置為使用 UTC 時間戳記,格式為 `yyyy-MM-ddTHH:mm:ss.fffZ`。

## 相關文件

- [功能規格](spec.md) - 完整的需求說明
- [實作計畫](plan.md) - 技術方案和架構設計
- [任務清單](tasks.md) - 開發任務分解
- [API 合約](contracts/) - 詳細的 API 規範

## 支援

如有任何問題或建議,請聯繫 V3 Admin Backend Team。
