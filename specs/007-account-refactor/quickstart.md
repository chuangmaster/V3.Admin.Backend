# Quickstart Guide: Account Module Refactoring

**Feature**: Account Module Refactoring  
**Branch**: `007-account-refactor`  
**Date**: 2026-01-20

## 🎯 Overview

本指南幫助開發者快速實作 Account Module Refactoring 功能,包含資料庫遷移、API 端點開發、測試撰寫等完整流程。

**預計開發時間**: 2-3 個工作日

---

## 📋 Prerequisites

在開始之前,請確認:

- [x] 已切換到 `007-account-refactor` 分支
- [x] 已閱讀 [spec.md](spec.md) 了解功能需求
- [x] 已閱讀 [plan.md](plan.md) 了解技術方案
- [x] 已閱讀 [data-model.md](data-model.md) 了解資料模型變更
- [x] 本地開發環境可正常執行現有測試
- [x] 可連接到測試用 PostgreSQL 資料庫

---

## 🗂️ Implementation Roadmap

### Phase 1: 資料庫遷移 (0.5 天)

#### 1.1 建立 Migration Scripts

在 `Database/Migrations/` 目錄建立兩個 migration scripts:

**檔案 1**: `006_RenameUsernameToAccount.sql`

```sql
-- Migration: Rename username field to account
-- Purpose: Improve module identification and semantic clarity
-- Date: 2026-01-20

BEGIN;

-- Step 1: Rename column
ALTER TABLE users RENAME COLUMN username TO account;

-- Step 2: Rename index (if exists)
ALTER INDEX IF EXISTS idx_users_username RENAME TO idx_users_account;

-- Step 3: Data integrity check
DO $$
DECLARE
    total_count INT;
    null_account_count INT;
BEGIN
    SELECT COUNT(*) INTO total_count FROM users;
    SELECT COUNT(*) INTO null_account_count FROM users WHERE account IS NULL OR account = '';
    
    IF null_account_count > 0 THEN
        RAISE EXCEPTION 'Data integrity check failed: % users have NULL or empty account', null_account_count;
    END IF;
    
    RAISE NOTICE 'Migration successful: % users migrated', total_count;
END $$;

COMMIT;
```

#### 1.2 執行 Migrations

```bash
# 在本地測試資料庫執行
psql -h localhost -U your_user -d v3_admin_backend_dev -f Database/Migrations/006_RenameUsernameToAccount.sql

# 驗證遷移結果
psql -h localhost -U your_user -d v3_admin_backend_dev -c "\d users"
```

#### 1.3 更新 User Entity

修改 `Models/Entities/User.cs`:

```csharp
// 將 Username 屬性重命名為 Account
[Column("account")]
public string Account { get; set; } = string.Empty;
```

---

### Phase 2: Repository 層更新 (0.5 天)

#### 2.1 更新 UserRepository

修改 `Repositories/UserRepository.cs` 和 `Repositories/Interfaces/IUserRepository.cs`:

```csharp
// IUserRepository.cs 新增方法
Task<bool> UpdatePasswordAsync(int userId, string hashedPassword, int expectedVersion);
Task<bool> ResetPasswordAsync(int userId, string hashedPassword, int expectedVersion);
Task<User?> GetByIdWithVersionAsync(int userId);
```

```csharp
// UserRepository.cs 實作
public async Task<bool> UpdatePasswordAsync(int userId, string hashedPassword, int expectedVersion)
{
    const string sql = @"
        UPDATE users 
        SET password = @Password, 
            version = version + 1,
            updated_at = NOW()
        WHERE id = @UserId AND version = @Version AND deleted_at IS NULL
        RETURNING version";

    var newVersion = await _connection.QuerySingleOrDefaultAsync<int?>(
        sql, 
        new { UserId = userId, Password = hashedPassword, Version = expectedVersion }
    );

    return newVersion.HasValue;
}

public async Task<bool> ResetPasswordAsync(int userId, string hashedPassword, int expectedVersion)
{
    // 與 UpdatePasswordAsync 相同實作
    return await UpdatePasswordAsync(userId, hashedPassword, expectedVersion);
}

public async Task<User?> GetByIdWithVersionAsync(int userId)
{
    const string sql = @"
        SELECT id, account, password, email, display_name, version, 
               is_active, created_at, updated_at, deleted_at
        FROM users 
        WHERE id = @UserId AND deleted_at IS NULL";

    return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
}
```

#### 2.2 更新 AuditLogRepository

確認 `Repositories/AuditLogRepository.cs` 有 CreateAsync 方法:

```csharp
public async Task<int> CreateAsync(AuditLog auditLog)
{
    const string sql = @"
        INSERT INTO audit_logs (action, operator_id, target_user_id, details, ip_address, created_at)
        VALUES (@Action, @OperatorId, @TargetUserId, @Details, @IpAddress, @CreatedAt)
        RETURNING id";

    return await _connection.QuerySingleAsync<int>(sql, auditLog);
}
```

---

### Phase 3: Service 層開發 (1 天)

#### 3.1 更新 IAccountService

在 `Services/Interfaces/IAccountService.cs` 新增方法:

```csharp
/// <summary>
/// 用戶修改自己的密碼
/// </summary>
Task<ApiResponseModel<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request);

/// <summary>
/// 管理員重設用戶密碼
/// </summary>
Task<ApiResponseModel<object>> ResetPasswordAsync(int operatorId, int targetUserId, ResetPasswordRequest request);
```

#### 3.2 實作 AccountService

在 `Services/AccountService.cs` 實作:

```csharp
public async Task<ApiResponseModel<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
{
    // 1. 查詢用戶
    var user = await _userRepository.GetByIdWithVersionAsync(userId);
    if (user == null)
    {
        return ApiResponse.Error<object>(ResponseCodes.USER_NOT_FOUND, "用戶不存在");
    }

    // 2. 驗證舊密碼
    if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
    {
        _logger.LogWarning("User {UserId} provided incorrect old password", userId);
        throw new UnauthorizedAccessException("舊密碼錯誤");
    }

    // 3. 驗證新密碼不同於舊密碼
    if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.Password))
    {
        return ApiResponse.Error<object>(ResponseCodes.SAME_PASSWORD, "新密碼不可與舊密碼相同");
    }

    // 4. 加密新密碼
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

    // 5. 更新密碼(併發控制)
    var updated = await _userRepository.UpdatePasswordAsync(userId, hashedPassword, request.Version);
    if (!updated)
    {
        _logger.LogWarning("Concurrent update conflict for user {UserId}, version {Version}", 
            userId, request.Version);
        return ApiResponse.Error<object>(
            ResponseCodes.CONCURRENT_UPDATE_CONFLICT,
            "密碼修改失敗,資料已被其他操作更新,請重新獲取最新資料後再試"
        );
    }

    _logger.LogInformation("User {UserId} changed password successfully", userId);
    return ApiResponse.Success<object>(null, "密碼修改成功");
}

public async Task<ApiResponseModel<object>> ResetPasswordAsync(
    int operatorId, int targetUserId, ResetPasswordRequest request)
{
    // 1. 查詢目標用戶
    var targetUser = await _userRepository.GetByIdWithVersionAsync(targetUserId);
    if (targetUser == null)
    {
        return ApiResponse.Error<object>(ResponseCodes.USER_NOT_FOUND, $"找不到 ID 為 {targetUserId} 的用戶");
    }

    // 2. 加密新密碼
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

    // 3. 重設密碼(併發控制)
    var updated = await _userRepository.ResetPasswordAsync(targetUserId, hashedPassword, request.Version);
    if (!updated)
    {
        _logger.LogWarning(
            "Concurrent update conflict when operator {OperatorId} resets password for user {TargetUserId}", 
            operatorId, targetUserId);
        return ApiResponse.Error<object>(
            ResponseCodes.CONCURRENT_UPDATE_CONFLICT,
            "密碼重設失敗,資料已被其他操作更新,請重新獲取最新資料後再試"
        );
    }

    // 4. 記錄審計日誌
    await _auditLogRepository.CreateAsync(new AuditLog
    {
        Action = "PasswordReset",
        OperatorId = operatorId,
        TargetUserId = targetUserId,
        Details = JsonSerializer.Serialize(new
        {
            Timestamp = DateTime.UtcNow
        }),
        CreatedAt = DateTime.UtcNow
    });

    _logger.LogInformation("Operator {OperatorId} reset password for user {TargetUserId}", 
        operatorId, targetUserId);
    return ApiResponse.Success<object>(null, "密碼重設成功");
}
```

---

### Phase 4: Controller 層開發 (0.5 天)

#### 4.1 更新 AccountController

在 `Controllers/AccountController.cs` 新增端點:

```csharp
/// <summary>
/// 用戶修改自己的密碼
/// </summary>
/// <param name="request">密碼修改請求</param>
/// <returns>操作結果</returns>
[HttpPut("me/password")]
[Authorize(Policy = "user.profile.update")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
{
    var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
    if (userId == 0)
    {
        return Unauthorized(ApiResponse.Error<object>(ResponseCodes.UNAUTHORIZED, "未授權,請重新登入"));
    }

    var result = await _accountService.ChangePasswordAsync(userId, request);
    return result.Code == 200 ? Ok(result) : StatusCode(result.Code, result);
}

/// <summary>
/// 管理員重設用戶密碼
/// </summary>
/// <param name="id">目標用戶 ID</param>
/// <param name="request">密碼重設請求</param>
/// <returns>操作結果</returns>
[HttpPut("{id}/reset-password")]
[Authorize(Policy = "account.update")]
public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
{
    var operatorId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
    if (operatorId == 0)
    {
        return Unauthorized(ApiResponse.Error<object>(ResponseCodes.UNAUTHORIZED, "未授權,請重新登入"));
    }

    var result = await _accountService.ResetPasswordAsync(operatorId, id, request);
    return result.Code == 200 ? Ok(result) : StatusCode(result.Code, result);
}
```

---

### Phase 5: Validators (0.5 天)

#### 5.1 建立 ChangePasswordRequestValidator

在 `Validators/ChangePasswordRequestValidator.cs`:

```csharp
using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators
{
    /// <summary>
    /// 密碼修改請求驗證器
    /// </summary>
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("舊密碼為必填");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("新密碼為必填")
                .MinimumLength(8).WithMessage("密碼長度至少 8 個字元")
                .MaximumLength(100).WithMessage("密碼長度最多 100 個字元")
                .Matches(@"[A-Z]").WithMessage("密碼必須包含至少一個大寫字母")
                .Matches(@"[a-z]").WithMessage("密碼必須包含至少一個小寫字母")
                .Matches(@"[0-9]").WithMessage("密碼必須包含至少一個數字");

            RuleFor(x => x.Version)
                .GreaterThanOrEqualTo(0).WithMessage("版本號必須大於或等於 0");
        }
    }
}
```

#### 5.2 建立 ResetPasswordRequestValidator

在 `Validators/ResetPasswordRequestValidator.cs`:

```csharp
using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators
{
    /// <summary>
    /// 密碼重設請求驗證器
    /// </summary>
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("新密碼為必填")
                .MinimumLength(8).WithMessage("密碼長度至少 8 個字元")
                .MaximumLength(100).WithMessage("密碼長度最多 100 個字元")
                .Matches(@"[A-Z]").WithMessage("密碼必須包含至少一個大寫字母")
                .Matches(@"[a-z]").WithMessage("密碼必須包含至少一個小寫字母")
                .Matches(@"[0-9]").WithMessage("密碼必須包含至少一個數字");

            RuleFor(x => x.Version)
                .GreaterThanOrEqualTo(0).WithMessage("版本號必須大於或等於 0");
        }
    }
}
```

#### 5.3 註冊 Validators

在 `Program.cs` 中註冊:

```csharp
builder.Services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
builder.Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();
```

---

### Phase 6: JWT Version 驗證 (0.5 天)

#### 6.1 更新 JwtService

在 `Services/JwtService.cs` 的 GenerateToken 方法中新增 version claim:

```csharp
var claims = new[]
{
    new Claim("user_id", user.Id.ToString()),
    new Claim("version", user.Version.ToString()), // 新增此行
    new Claim("account", user.Account),
    new Claim(ClaimTypes.Email, user.Email),
    // ... 其他 claims
};
```

#### 6.2 建立 Version 驗證 Middleware

在 `Middleware/VersionValidationMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace V3.Admin.Backend.Middleware
{
    /// <summary>
    /// Version 驗證中介軟體
    /// 驗證 JWT 中的 version 與資料庫當前 version 是否一致
    /// 任何資料修改都會遞增 version,使舊 token 失效
    /// </summary>
    public class VersionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public VersionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("user_id")?.Value;
                var versionClaim = context.User.FindFirst("version")?.Value;

                if (int.TryParse(userIdClaim, out var userId) && 
                    int.TryParse(versionClaim, out var version))
                {
                    var user = await userRepository.GetByIdWithVersionAsync(userId);
                    if (user != null && user.Version != version)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            code = 401,
                            message = "Token 已失效,請重新登入",
                            errors = new[] { "用戶資料已被修改,請重新登入" }
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
```

在 `Program.cs` 註冊 middleware:

```csharp
app.UseAuthentication();
app.UseMiddleware<VersionValidationMiddleware>(); // 在 UseAuthorization 之前
app.UseAuthorization();
```

---

### Phase 7: 測試撰寫 (1-1.5 天)

#### 7.1 Validator 測試 (100% 覆蓋率要求)

在 `Tests/Unit/Validators/ChangePasswordRequestValidatorTests.cs`:

```csharp
public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator;

    public ChangePasswordRequestValidatorTests()
    {
        _validator = new ChangePasswordRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            Version = 1
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOldPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "",
            NewPassword = "NewPassword456",
            Version = 1
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.OldPassword));
    }

    [Theory]
    [InlineData("short")]           // 太短
    [InlineData("nouppercase123")]  // 無大寫
    [InlineData("NOLOWERCASE123")]  // 無小寫
    [InlineData("NoNumber")]        // 無數字
    public void Validate_WeakPassword_ShouldFail(string weakPassword)
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = weakPassword,
            Version = 1
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.NewPassword));
    }

    [Fact]
    public void Validate_NegativeVersion_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            Version = -1
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Version));
    }
}
```

#### 7.2 Service 單元測試

在 `Tests/Unit/Services/AccountServiceTests.cs`:

```csharp
public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAuditLogRepository> _mockAuditLogRepository;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAuditLogRepository = new Mock<IAuditLogRepository>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _service = new AccountService(_mockUserRepository.Object, _mockAuditLogRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var oldPassword = "OldPassword123";
        var newPassword = "NewPassword456";
        var hashedOldPassword = BCrypt.Net.BCrypt.HashPassword(oldPassword, 12);

        var user = new User
        {
            Id = userId,
            Password = hashedOldPassword,
            Version = 1
        };

        _mockUserRepository.Setup(r => r.GetByIdWithVersionAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdatePasswordAsync(userId, It.IsAny<string>(), 1))
            .ReturnsAsync(true);

        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword,
            Version = 1
        };

        // Act
        var result = await _service.ChangePasswordAsync(userId, request);

        // Assert
        result.Code.Should().Be(200);
        result.Message.Should().Be("密碼修改成功");
        _mockUserRepository.Verify(r => r.UpdatePasswordAsync(userId, It.IsAny<string>(), 1), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_IncorrectOldPassword_ShouldFail()
    {
        // Arrange
        var userId = 1;
        var oldPassword = "CorrectOldPassword123";
        var wrongOldPassword = "WrongPassword123";
        var hashedOldPassword = BCrypt.Net.BCrypt.HashPassword(oldPassword, 12);

        var user = new User
        {
            Id = userId,
            Password = hashedOldPassword,
            Version = 1
        };

        _mockUserRepository.Setup(r => r.GetByIdWithVersionAsync(userId))
            .ReturnsAsync(user);

        var request = new ChangePasswordRequest
        {
            OldPassword = wrongOldPassword,
            NewPassword = "NewPassword456",
            Version = 1
        };

        // Act
        var act = async () => await _service.ChangePasswordAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("舊密碼錯誤");
        _mockUserRepository.Verify(r => r.UpdatePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_VersionMismatch_ShouldReturnConflict()
    {
        // Arrange
        var userId = 1;
        var oldPassword = "OldPassword123";
        var hashedOldPassword = BCrypt.Net.BCrypt.HashPassword(oldPassword, 12);

        var user = new User
        {
            Id = userId,
            Password = hashedOldPassword,
            Version = 2
        };

        _mockUserRepository.Setup(r => r.GetByIdWithVersionAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdatePasswordAsync(userId, It.IsAny<string>(), 1))
            .ReturnsAsync(false); // Version mismatch

        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = "NewPassword456",
            Version = 1
        };

        // Act
        var result = await _service.ChangePasswordAsync(userId, request);

        // Assert
        result.Code.Should().Be(ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
    }

    // 繼續新增更多測試案例...
}
```

#### 7.3 Integration 測試 (使用 Testcontainers)

在 `Tests/Integration/Controllers/AccountControllerTests.cs`:

```csharp
public class AccountControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public AccountControllerTests()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替換資料庫連接字串為測試容器
                    var dbSettings = services.FirstOrDefault(s => s.ServiceType == typeof(DatabaseSettings));
                    if (dbSettings != null)
                    {
                        services.Remove(dbSettings);
                    }
                    services.AddSingleton(new DatabaseSettings
                    {
                        ConnectionString = _dbContainer.GetConnectionString()
                    });
                });
            });

        _client = _factory.CreateClient();

        // 執行資料庫遷移
        await RunMigrationsAsync();
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ShouldReturn200()
    {
        // Arrange
        var userId = await CreateTestUserAsync("testuser", "OldPassword123");
        var token = await GetJwtTokenAsync(userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword456",
            Version = 0
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/account/me/password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponseModel<object>>();
        result.Code.Should().Be(200);
        result.Message.Should().Be("密碼修改成功");
    }

    // 繼續新增更多整合測試...

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        _factory?.Dispose();
        _client?.Dispose();
    }
}
```

---

## ✅ Checklist

開發完成前,請確認以下檢查項目:

### 資料庫
- [ ] Migration scripts 已建立且可正確執行
- [ ] 欄位重命名成功,無資料遺失
- [ ] 資料表欄位 username 已重命名為 account
- [ ] 索引已正確重命名

### 程式碼
- [ ] User Entity 已更新(Username → Account)
- [ ] UserRepository 新增 UpdatePasswordAsync, ResetPasswordAsync 方法
- [ ] AccountService 實作 ChangePasswordAsync, ResetPasswordAsync
- [ ] AccountController 新增兩個端點
- [ ] Validators 已建立並註冊
- [ ] VersionValidationMiddleware 已建立並註冊
- [ ] JwtService 在 token 中包含 version claim

### 測試
- [ ] ChangePasswordRequestValidator 測試覆蓋率 100%
- [ ] ResetPasswordRequestValidator 測試覆蓋率 100%
- [ ] AccountService 單元測試涵蓋主要場景
- [ ] Integration 測試驗證端到端流程
- [ ] 測試併發衝突場景(version mismatch)
- [ ] 所有測試通過

### 文件
- [ ] 兩個端點的 XML 註解已完成
- [ ] API 文件已更新(OpenAPI spec)
- [ ] 資料庫 migration 腳本包含註解

---

## 🚀 Testing Commands

```bash
# 執行所有測試
dotnet test

# 執行單元測試
dotnet test --filter "FullyQualifiedName~Unit"

# 執行整合測試
dotnet test --filter "FullyQualifiedName~Integration"

# 執行特定測試類別
dotnet test --filter "FullyQualifiedName~AccountServiceTests"

# 測試覆蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 🐛 Troubleshooting

### 問題 1: Migration 執行失敗

**錯誤**: `column "username" does not exist`

**解決方案**: 檢查 migration 執行順序,確保 006_RenameUsernameToAccount.sql 先於任何依賴 account 欄位的 migration 執行。

### 問題 2: 併發衝突未生效

**錯誤**: 兩個請求都成功更新

**解決方案**: 確認 SQL 語句包含 `WHERE version = @Version`,並且使用 `RETURNING version` 驗證更新結果。

### 問題 3: Token version 驗證不生效

**錯誤**: 密碼修改後,舊 token 仍然有效

**解決方案**: 
1. 確認 JwtService 在生成 token 時包含 version claim
2. 確認 VersionValidationMiddleware 已註冊且位於正確位置
3. 檢查 middleware 是否正確查詢資料庫中的 version

---

## 📚 Related Documentation

- [Feature Specification](spec.md)
- [Implementation Plan](plan.md)
- [Data Model](data-model.md)
- [API Contracts](contracts/api-spec.yaml)
- [Project Constitution](../../.specify/memory/constitution.md)

---

## 🎉 Next Steps

完成所有檢查項目後:

1. 執行完整測試套件,確保所有測試通過
2. 提交 PR,等待 code review
3. 合併後部署到 staging 環境驗證
4. 部署到生產環境(注意:需先執行 migrations)

**預估總開發時間**: 2-3 個工作日
