# 技術研究與決策 - 帳號管理系統

**Feature**: 001-account-management  
**Date**: 2025-10-26  
**Status**: Phase 0 Complete

## 概述

本文件記錄帳號管理系統開發過程中的技術選擇、研究發現與決策依據。所有技術決策均基於專案憲法要求、效能目標與安全性考量。

---

## 1. 資料庫技術選擇

### 決策: PostgreSQL + Dapper

**選擇原因**:
- **PostgreSQL**: 開源、成熟穩定、支援 ACID 事務、優秀的 JSON 支援、豐富的資料型態
- **Dapper**: 輕量級 micro-ORM、高效能 (接近原生 ADO.NET)、支援原生 SQL 控制、易於學習與維護
- 符合使用者需求「採用 PostgreSQL,並用 Dapper 做連結」

**技術整合**:
```csharp
// 使用 Npgsql 作為 PostgreSQL 驅動
// Dapper 擴充 IDbConnection 提供查詢方法
// 支援參數化查詢防止 SQL Injection
```

**替代方案與拒絕理由**:
- **Entity Framework Core**: 功能強大但較重量級,對於帳號管理這種相對簡單的 CRUD 操作可能過度工程化,且 Dapper 效能更優
- **MongoDB + NoSQL**: 帳號管理需要強 ACID 保證與關聯查詢,關聯式資料庫更適合
- **MySQL**: 功能相似但 PostgreSQL 在資料完整性、並發控制與擴充性上表現更佳

**最佳實踐**:
- 使用連接池 (connection pooling) 提升效能
- 實作 Repository Pattern 封裝資料存取邏輯
- 使用參數化查詢防止 SQL Injection
- 實作資料庫遷移 (migration) 管理架構變更
- 使用交易 (transaction) 確保資料一致性

---

## 2. 密碼雜湊策略

### 決策: BCrypt.Net-Next

**選擇原因**:
- **BCrypt**: 業界標準、自動加鹽 (salting)、可調整工作因子 (work factor) 應對硬體進步
- **BCrypt.Net-Next**: .NET 平台最活躍維護的 BCrypt 實作、支援 .NET 9
- 抵抗彩虹表 (rainbow table) 與暴力破解攻擊
- 符合專案憲法安全要求「密碼必須使用雜湊演算法而非明文儲存」

**實作細節**:
```csharp
// 雜湊密碼 (註冊/變更密碼時)
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

// 驗證密碼 (登入時)
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
```

**Work Factor 選擇**: 12 (平衡安全性與效能,約 200-300ms 驗證時間)

**替代方案與拒絕理由**:
- **SHA256/SHA512 + Salt**: 不足以抵抗 GPU 加速暴力破解,缺乏可調整的工作因子
- **Argon2**: 更現代但生態系較不成熟,.NET 實作較少且 BCrypt 已足夠安全
- **PBKDF2**: 可用但 BCrypt 在記憶體硬度上表現更佳

**最佳實踐**:
- Work factor 設定為 12 (可在組態檔調整)
- 儲存完整的 BCrypt 雜湊字串 (包含演算法版本、work factor、salt、hash)
- 定期審查 work factor 是否需要提升
- 密碼變更時重新雜湊,不可逆轉

---

## 3. JWT 身份驗證設計

### 決策: JWT Bearer Token + Refresh Token

**選擇原因**:
- **無狀態 (stateless)**: 適合 RESTful API,易於水平擴展
- **跨平台支援**: 前端 (JavaScript) 與後端 (.NET) 均有成熟的函式庫
- **標準化**: RFC 7519 標準,廣泛支援與互通性
- 符合專案憲法要求「JWT 身份驗證 (Bearer Token)」

**Token 結構設計**:
```json
{
  "sub": "user_id",           // Subject - 使用者 ID
  "unique_name": "username",  // 使用者名稱
  "name": "display_name",     // 顯示名稱
  "jti": "token_id",          // JWT ID - 用於撤銷
  "iat": 1234567890,          // Issued At
  "exp": 1234571490,          // Expiration (1 hour)
  "nbf": 1234567890           // Not Before
}
```

**Token 生命週期**:
- **Access Token**: 1 小時 (短期,限制洩漏風險)
- **Refresh Token**: 7 天 (長期,儲存於 HttpOnly Cookie 或安全儲存)
- **Session 無活動逾時**: 30 分鐘 (透過 middleware 監控)

**安全實作**:
- 使用 HMAC-SHA256 或 RS256 簽章
- Secret Key 儲存於環境變數或 Azure Key Vault
- 實作 token 撤銷機制 (黑名單或資料庫標記)
- 支援多裝置登入 (每個 session 獨立 token)

**替代方案與拒絕理由**:
- **Session Cookie**: 需要伺服器端狀態儲存,不利於水平擴展
- **OAuth 2.0**: 對於單體應用過於複雜,JWT 已足夠
- **API Key**: 缺乏過期機制與細緻的權限控制

**最佳實踐**:
- Token 透過 HTTPS 傳輸 (防止中間人攻擊)
- Refresh Token 儲存於 HttpOnly Cookie (防止 XSS)
- 實作 token 撤銷機制 (使用者登出或密碼變更時)
- 記錄所有 token 發放與使用事件供審計

---

## 4. 輸入驗證策略

### 決策: FluentValidation

**選擇原因**:
- **強類型驗證**: 編譯時期檢查,減少執行時期錯誤
- **可測試性**: 驗證器可獨立測試
- **可讀性**: 流暢的 API 設計,易於理解與維護
- **擴充性**: 支援自訂驗證規則與非同步驗證
- 符合專案憲法要求「所有使用者輸入必須使用 FluentValidation 或資料註解進行驗證」

**驗證規則範例**:
```csharp
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        // 帳號名稱: 3-20 字元,僅英數字與底線
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不可為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("帳號僅允許英數字與底線");
        
        // 密碼: 最少 8 字元,支援所有 Unicode
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不可為空")
            .MinimumLength(8).WithMessage("密碼長度至少 8 字元");
        
        // 姓名: 不可為空
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("姓名不可為空")
            .MaximumLength(100).WithMessage("姓名長度不可超過 100 字元");
    }
}
```

**整合方式**:
- 註冊至 DI 容器: `services.AddValidatorsFromAssemblyContaining<Program>()`
- 使用 Filter 或 Middleware 自動驗證請求
- 驗證失敗回傳 400 Bad Request + VALIDATION_ERROR 業務代碼

**替代方案與拒絕理由**:
- **Data Annotations**: 功能較弱,難以實作複雜的跨欄位驗證
- **手動驗證**: 重複程式碼多,難以維護與測試
- **第三方驗證函式庫**: FluentValidation 已是 .NET 生態系標準

**最佳實踐**:
- 每個 Request DTO 都有對應的 Validator
- 驗證器可獨立單元測試
- 錯誤訊息使用繁體中文,符合使用者體驗要求
- 實作非同步驗證 (例如檢查帳號是否已存在)

---

## 5. API 回應設計

### 決策: ApiResponseModel<T> 雙層設計

**選擇原因**:
- **雙層設計**: HTTP 狀態碼 + 業務邏輯代碼,提供細緻的錯誤分類
- **一致性**: 所有 API 端點使用統一的回應格式
- **可追蹤性**: TraceId 支援分散式追蹤與問題診斷
- 符合專案憲法要求「所有 API 回應必須使用 ApiResponseModel 包裝」

**回應結構**:
```csharp
public class ApiResponseModel<T>
{
    public bool Success { get; set; }              // 操作是否成功
    public string Code { get; set; }               // 業務邏輯代碼 (ResponseCodes)
    public string Message { get; set; }            // 繁體中文訊息
    public T? Data { get; set; }                   // 回應資料
    public DateTime Timestamp { get; set; }        // 回應時間戳記
    public string TraceId { get; set; }            // 追蹤 ID
}
```

**業務邏輯代碼範例**:
```csharp
public static class ResponseCodes
{
    // 成功
    public const string SUCCESS = "SUCCESS";
    public const string CREATED = "CREATED";
    
    // 驗證錯誤
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    
    // 業務邏輯錯誤
    public const string USERNAME_EXISTS = "USERNAME_EXISTS";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string LAST_ACCOUNT_CANNOT_DELETE = "LAST_ACCOUNT_CANNOT_DELETE";
    public const string PASSWORD_SAME_AS_OLD = "PASSWORD_SAME_AS_OLD";
    
    // 系統錯誤
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
}
```

**HTTP 狀態碼對應**:
- **200 OK**: 成功操作 (查詢、更新)
- **201 Created**: 成功建立資源 (新增帳號)
- **400 Bad Request**: 驗證錯誤
- **401 Unauthorized**: 身份驗證失敗
- **403 Forbidden**: 授權失敗 (權限不足)
- **404 Not Found**: 資源不存在
- **422 Unprocessable Entity**: 業務邏輯錯誤
- **500 Internal Server Error**: 系統錯誤

**最佳實踐**:
- Controller 實作輔助方法簡化回應建立
- 全域異常處理 Middleware 統一處理未預期錯誤
- TraceId 注入至所有回應 (透過 Middleware)
- 記錄所有錯誤回應供後續分析

---

## 6. 軟刪除機制

### 決策: 標記刪除 + 保留資料

**選擇原因**:
- **審計追蹤**: 保留完整的資料歷史供審計與合規
- **資料恢復**: 支援誤刪後的資料恢復
- **關聯完整性**: 保留關聯資料的參考完整性
- 符合規格要求「刪除帳號時採用軟刪除機制」

**實作設計**:
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }           // 軟刪除標記
    public DateTime? DeletedAt { get; set; }      // 刪除時間
    public Guid? DeletedBy { get; set; }          // 刪除操作者
}
```

**查詢過濾**:
- 所有查詢預設排除已刪除的帳號: `WHERE IsDeleted = false`
- Repository 層實作通用的過濾邏輯
- 需要包含已刪除資料時使用特殊查詢方法

**刪除操作**:
```csharp
// 軟刪除
UPDATE users 
SET IsDeleted = true, DeletedAt = @now, DeletedBy = @operatorId 
WHERE Id = @userId;

// 防止登入
WHERE Username = @username AND IsDeleted = false;
```

**最佳實踐**:
- 記錄刪除操作者與時間供審計
- 實作定期清理機制 (例如 1 年後永久刪除)
- 提供管理介面查看已刪除帳號
- 考慮 GDPR 等資料保護法規的永久刪除需求

---

## 7. 三層架構設計

### 決策: Controller / Service / Repository

**選擇原因**:
- **關注點分離**: 每層職責清晰,易於維護與測試
- **可測試性**: 各層可獨立單元測試
- **可替換性**: 可輕易替換資料存取層 (例如從 Dapper 換到 EF Core)
- 符合使用者需求「採用 DTO 的方式,並使用三層式架構」與專案憲法要求

**職責劃分**:

**Controller (展示層)**:
- 處理 HTTP 請求與回應
- 驗證輸入 (透過 FluentValidation)
- 呼叫 Service 層執行業務邏輯
- 將結果包裝為 ApiResponseModel 回應
- 不包含業務邏輯
- 使用 Request/Response DTO

**Service (業務邏輯層)**:
- 實作商業邏輯與規則驗證
- 協調多個 Repository 操作
- 處理交易管理
- 使用 Request/Response DTO 與 Controller 層溝通
- 使用 Entity 與 Repository 層溝通
- Entity ↔ DTO 轉換在此層完成

**Repository (資料存取層)**:
- 封裝資料庫操作 (CRUD)
- 使用 Dapper 執行 SQL 查詢
- 返回 Entity 物件 (單一表格映射)
- 返回 View 物件 (Join 查詢結果)
- 不包含業務邏輯

**DTO 命名規則**:
- **Entity**: 單一資料表映射 (例如: `User`, `Role`)
- **Request**: API 輸入模型 (例如: `CreateAccountRequest`)
- **Response**: API 輸出模型 (例如: `AccountResponse`)
- **View**: Join 查詢結果 (例如: `UserRoleView`)

**範例流程**:
```
Client → Controller (Request DTO) 
       → Validator (驗證)
       → Service (業務邏輯, Request → Entity)
       → Repository (Entity/View, SQL)
       → Database
       ← Repository (Entity/View)
       ← Service (Entity/View → Response)
       ← Controller (ApiResponseModel<Response DTO>)
       ← Client
```

**最佳實踐**:
- 介面導向設計,所有 Service 與 Repository 定義介面
- 依賴注入管理所有相依性
- 每層負責自己的資料轉換 (DTO ↔ Entity)
- 使用 async/await 提升 I/O 密集操作效能

---

## 8. 並發控制策略

### 決策: 樂觀並發控制 (Optimistic Concurrency)

**選擇原因**:
- 帳號管理操作通常不會有高並發衝突
- 樂觀控制效能優於悲觀鎖定
- 適合分散式環境與水平擴展

**實作方式**:
```csharp
public class User
{
    // ... 其他欄位
    public int Version { get; set; }  // 版本號或時間戳記
}

// 更新時檢查版本
UPDATE users 
SET DisplayName = @name, Version = Version + 1, UpdatedAt = @now
WHERE Id = @id AND Version = @expectedVersion;

// 檢查影響行數,若為 0 表示並發衝突
```

**衝突處理**:
- 偵測到衝突時回傳 409 Conflict
- 提示使用者重新載入並再次嘗試
- 記錄並發衝突事件供分析

**特殊場景 - 最後一個帳號保護**:
```sql
-- 刪除前檢查是否為最後一個有效帳號
SELECT COUNT(*) FROM users WHERE IsDeleted = false;
-- 若 count <= 1 則拒絕刪除
```

**最佳實踐**:
- 關鍵操作使用資料庫交易確保 ACID
- 記錄所有並發衝突事件
- 提供清晰的錯誤訊息協助使用者處理

---

## 9. 效能優化策略

### 決策: 連接池 + 索引 + 快取

**資料庫連接池**:
```csharp
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=v3admin;Username=admin;Password=***;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;"
}
```

**資料庫索引**:
```sql
-- 主鍵索引 (自動)
PRIMARY KEY (Id)

-- 帳號名稱唯一索引 (用於登入查詢與唯一性檢查)
CREATE UNIQUE INDEX idx_users_username ON users(Username) WHERE IsDeleted = false;

-- 複合索引 (用於刪除帳號計數查詢)
CREATE INDEX idx_users_isdeleted ON users(IsDeleted);
```

**查詢優化**:
- 使用參數化查詢 (Dapper 預設支援)
- 避免 SELECT *,明確指定需要的欄位
- 使用分頁 (LIMIT/OFFSET) 處理大量資料

**快取策略 (未來擴充)**:
- 使用 IMemoryCache 快取常用查詢 (例如活躍帳號列表)
- 快取 TTL 設定為 5-10 分鐘
- 資料變更時清除相關快取

**效能監控**:
- 使用 Application Insights 或 Prometheus 監控 API 回應時間
- 記錄慢查詢 (>100ms) 供優化
- 定期審查資料庫執行計畫

**效能目標**:
- 登入操作: <200ms (包含密碼驗證)
- 新增/更新帳號: <200ms
- 刪除帳號: <200ms
- 查詢帳號列表: <2000ms (含分頁)

---

## 10. 測試策略

### 決策: 測試金字塔 (Unit > Integration > E2E)

**單元測試 (Unit Tests)**:
- 目標: Service 層業務邏輯、Validator 驗證規則
- 工具: xUnit + Moq + FluentAssertions
- 覆蓋率目標: >80%
- 範例:
  ```csharp
  [Fact]
  public async Task CreateAccount_WhenUsernameExists_ShouldThrowException()
  {
      // Arrange
      var mockRepo = new Mock<IUserRepository>();
      mockRepo.Setup(r => r.ExistsAsync(It.IsAny<string>()))
              .ReturnsAsync(true);
      var service = new AccountService(mockRepo.Object);
      
      // Act & Assert
      await Assert.ThrowsAsync<BusinessException>(
          () => service.CreateAccountAsync(new CreateAccountRequest { ... })
      );
  }
  ```

**整合測試 (Integration Tests)**:
- 目標: API 端點、資料庫操作
- 工具: Microsoft.AspNetCore.Mvc.Testing + Testcontainers (PostgreSQL)
- 測試實際 HTTP 請求/回應與資料庫互動
- 範例:
  ```csharp
  public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
  {
      [Fact]
      public async Task Login_WithValidCredentials_ShouldReturnToken()
      {
          // Arrange
          var client = _factory.CreateClient();
          
          // Act
          var response = await client.PostAsJsonAsync("/api/auth/login", new { ... });
          
          // Assert
          response.StatusCode.Should().Be(HttpStatusCode.OK);
          var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<LoginResponse>>();
          result.Success.Should().BeTrue();
          result.Data.Token.Should().NotBeNullOrEmpty();
      }
  }
  ```

**契約測試 (Contract Tests)**:
- 驗證 API 符合 OpenAPI 規格
- 使用 Swashbuckle.AspNetCore.Cli 生成規格
- 前後端團隊共同審查契約

**測試資料管理**:
- 使用 seed.sql 建立測試資料
- 每個測試獨立交易,結束後 rollback
- 使用 Testcontainers 提供隔離的測試資料庫

**最佳實踐**:
- 測試先行開發 (TDD)
- 測試名稱清晰描述情境與預期結果
- 使用 Given-When-Then 結構
- 測試應快速執行 (<5 秒整體)
- CI/CD 流程自動執行所有測試

---

## 11. 錯誤處理與日誌記錄

### 決策: 全域異常處理 + 結構化日誌

**全域異常處理 Middleware**:
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            // 業務邏輯異常 (例如帳號已存在)
            logger.LogWarning(ex, "Business error occurred");
            await HandleBusinessExceptionAsync(context, ex);
        }
        catch (ValidationException ex)
        {
            // 驗證異常
            logger.LogWarning(ex, "Validation error occurred");
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            // 未預期的系統異常
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }
}
```

**日誌記錄策略**:
- 使用 Serilog 結構化日誌
- 日誌等級:
  - **Trace**: 詳細的流程追蹤 (開發環境)
  - **Debug**: 除錯資訊 (開發環境)
  - **Information**: 一般資訊 (登入成功、帳號建立)
  - **Warning**: 警告事件 (登入失敗、業務邏輯錯誤)
  - **Error**: 錯誤事件 (系統異常、資料庫錯誤)
  - **Critical**: 嚴重錯誤 (系統無法運作)

**日誌格式**:
```csharp
logger.LogInformation(
    "User {Username} logged in successfully from {IpAddress} with TraceId {TraceId}",
    username, ipAddress, traceId
);

logger.LogWarning(
    "Failed login attempt for user {Username} from {IpAddress}. Reason: {Reason}",
    username, ipAddress, reason
);
```

**敏感資訊保護**:
- 絕不記錄密碼 (明文或雜湊)
- 遮罩個人資訊 (例如僅記錄部分 email)
- 生產環境不記錄完整的請求/回應內容

**日誌輸出**:
- 開發環境: Console + File
- 生產環境: File + Application Insights / ELK Stack
- 保留 30 天日誌供審計

**最佳實踐**:
- 使用結構化日誌 (JSON 格式)
- 包含 TraceId 關聯所有相關日誌
- 記錄所有安全相關事件 (登入、權限變更)
- 定期審查日誌尋找異常模式

---

## 12. 部署與環境設定

### 決策: 多環境設定 + Docker 容器化

**環境分離**:
- **Development**: 本機開發,使用 appsettings.Development.json
- **Staging**: 測試環境,模擬生產設定
- **Production**: 生產環境,使用環境變數或 Azure App Configuration

**設定管理**:
```json
// appsettings.json (基礎設定)
{
  "Jwt": {
    "SecretKey": "OVERRIDE_IN_PRODUCTION",
    "Issuer": "V3.Admin.Backend",
    "Audience": "V3.Admin.Frontend",
    "ExpirationMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "OVERRIDE_IN_PRODUCTION"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

// 生產環境使用環境變數覆寫
JWT__SECRETKEY=<strong-secret-from-key-vault>
CONNECTIONSTRINGS__DEFAULTCONNECTION=<production-db-connection>
```

**Docker 容器化**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["V3.Admin.Backend.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "V3.Admin.Backend.dll"]
```

**資料庫遷移**:
- 使用 SQL 遷移腳本管理架構變更
- 編號命名: `001_CreateUsersTable.sql`, `002_AddIndexes.sql`
- 部署前自動執行遷移 (或手動審查後執行)

**最佳實踐**:
- 敏感設定使用環境變數或 Key Vault
- 設定檔不包含密碼或金鑰
- 使用 Docker Compose 本機開發 (API + PostgreSQL)
- CI/CD 自動化建置與部署流程

---

## 13. 相依性注入設計

### 決策: 介面導向 + DI 容器

**註冊範例 (Program.cs)**:
```csharp
// 資料庫連接
builder.Services.AddScoped<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // JWT 設定
    });

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
```

**生命週期選擇**:
- **Scoped**: Repository, Service, DbConnection (每個請求一個實例)
- **Singleton**: Configuration, Logger (應用程式生命週期)
- **Transient**: Validator (每次注入新實例)

**最佳實踐**:
- 所有相依性透過建構函式注入
- 避免使用 ServiceLocator 反模式
- 介面放置於 Interfaces 資料夾
- 使用 IOptions<T> 注入強類型設定

---

## 總結

本技術研究文件涵蓋帳號管理系統的所有關鍵技術決策,所有選擇均基於專案憲法要求、使用者需求與業界最佳實踐。主要技術堆疊包括:

- **.NET 9 + ASP.NET Core** (Web API)
- **PostgreSQL + Dapper** (資料存取)
- **BCrypt** (密碼雜湊)
- **JWT** (身份驗證)
- **FluentValidation** (輸入驗證)
- **三層架構** (Controller/Service/Repository)
- **xUnit + Moq** (測試)

所有技術決策已解決 Technical Context 中的 NEEDS CLARIFICATION 項目,可進入 Phase 1 設計階段。

**下一步**: 建立 `data-model.md` 定義資料模型與 `contracts/api-spec.yaml` 定義 API 合約。
