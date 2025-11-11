# Quickstart: 用戶個人資料查詢 API

**Feature**: 003-user-profile  
**Date**: 2025-11-12  
**Purpose**: 快速開始開發指南

## 開發環境設定

### 前置需求
- .NET 9 SDK
- PostgreSQL 資料庫
- Visual Studio 2022 或 VS Code
- Postman 或類似的 API 測試工具

### 專案設定
```bash
# 1. 切換到功能分支
git checkout 003-user-profile

# 2. 還原套件
dotnet restore

# 3. 確認資料庫連線
# 編輯 appsettings.development.json 中的 DatabaseSettings

# 4. 執行專案
dotnet run
```

---

## 開發步驟

### Step 1: 新增權限定義

**檔案**: `Database/Scripts/seed_permissions.sql`

```sql
-- 在檔案末尾新增
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('user.profile.read', '查詢個人資料', '允許查詢當前用戶的個人資料', 'function')
ON CONFLICT (permission_code) DO NOTHING;
```

**執行**:
```bash
# 使用 psql 或其他資料庫工具執行
psql -U your_user -d your_database -f Database/Scripts/seed_permissions.sql
```

---

### Step 2: 新增 Response DTO

**檔案**: `Models/Responses/UserProfileResponse.cs`

```csharp
namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 用戶個人資料回應 DTO
/// </summary>
public class UserProfileResponse
{
    /// <summary>
    /// 用戶名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 顯示名稱（可為 null）
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 角色名稱清單
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
```

---

### Step 3: 擴展 Repository 方法（如需要）

**檔案**: `Repositories/Interfaces/IUserRoleRepository.cs`

檢查是否已有 `GetRoleNamesByUserIdAsync` 方法，若無則新增：

```csharp
/// <summary>
/// 根據用戶 ID 取得角色名稱清單
/// </summary>
Task<List<string>> GetRoleNamesByUserIdAsync(int userId);
```

**檔案**: `Repositories/UserRoleRepository.cs`

```csharp
public async Task<List<string>> GetRoleNamesByUserIdAsync(int userId)
{
    const string sql = @"
        SELECT r.name
        FROM user_roles ur
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE ur.user_id = @UserId 
            AND ur.is_deleted = false 
            AND r.is_deleted = false";

    using var connection = _dbContext.CreateConnection();
    var roleNames = await connection.QueryAsync<string>(sql, new { UserId = userId });
    return roleNames.ToList();
}
```

---

### Step 4: 擴展 Service 介面與實作

**檔案**: `Services/Interfaces/IAccountService.cs`

```csharp
/// <summary>
/// 取得用戶個人資料
/// </summary>
Task<UserProfileResponse?> GetUserProfileAsync(Guid userId);
```

**檔案**: `Services/AccountService.cs`

```csharp
public async Task<UserProfileResponse?> GetUserProfileAsync(Guid userId)
{
    // 1. 查詢用戶基本資訊
    var user = await _userRepository.GetUserByIdAsync(userId);
    if (user is null || user.IsDeleted)
    {
        return null;
    }

    // 2. 查詢用戶角色
    var roleNames = await _userRoleRepository.GetRoleNamesByUserIdAsync(userId);

    // 3. 組合回應
    return new UserProfileResponse
    {
        Username = user.Username,
        DisplayName = user.DisplayName,
        Roles = roleNames
    };
}
```

---

### Step 5: 新增 Controller 端點

**檔案**: `Controllers/AccountController.cs`

```csharp
/// <summary>
/// 查詢當前用戶的個人資料
/// </summary>
/// <returns>用戶個人資料</returns>
/// <response code="200">查詢成功</response>
/// <response code="401">未授權</response>
/// <response code="403">無權限</response>
/// <response code="404">用戶不存在</response>
[HttpGet("me")]
[RequirePermission("user.profile.read")]
[ProducesResponseType(typeof(ApiResponseModel<UserProfileResponse>), 200)]
[ProducesResponseType(typeof(ApiResponseModel), 401)]
[ProducesResponseType(typeof(ApiResponseModel), 403)]
[ProducesResponseType(typeof(ApiResponseModel), 404)]
public async Task<IActionResult> GetMyProfile()
{
    try
    {
        // 從 JWT token 取得用戶 ID (使用 BaseApiController 的方法)
        var userId = GetUserId();
        if (userId is null)
        {
            return UnauthorizedResponse("未授權，請先登入");
        }

        // 查詢用戶資料
        var profile = await _accountService.GetUserProfileAsync(userId.Value);
        
        if (profile is null)
        {
            return NotFound("用戶不存在");
        }

        return Success(profile, "查詢成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "查詢用戶個人資料時發生錯誤");
        return InternalError("系統內部錯誤，請稍後再試");
    }
}
```

---

### Step 6: 撰寫測試

**檔案**: `Tests/Unit/AccountServiceTests.cs`

```csharp
[Fact]
public async Task GetUserProfileAsync_WithValidUser_ReturnsProfile()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockUser = new User 
    { 
        Id = userId, 
        Username = "test_user", 
        DisplayName = "Test User",
        IsDeleted = false
    };
    var mockRoles = new List<string> { "Admin", "User" };

    _mockUserRepository
        .Setup(x => x.GetUserByIdAsync(userId))
        .ReturnsAsync(mockUser);
    
    _mockUserRoleRepository
        .Setup(x => x.GetRoleNamesByUserIdAsync(userId))
        .ReturnsAsync(mockRoles);

    // Act
    var result = await _accountService.GetUserProfileAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test_user", result.Username);
    Assert.Equal("Test User", result.DisplayName);
    Assert.Equal(2, result.Roles.Count);
    Assert.Contains("Admin", result.Roles);
}

[Fact]
public async Task GetUserProfileAsync_WithDeletedUser_ReturnsNull()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockUser = new User 
    { 
        Id = userId, 
        Username = "deleted_user",
        IsDeleted = true
    };

    _mockUserRepository
        .Setup(x => x.GetUserByIdAsync(userId))
        .ReturnsAsync(mockUser);

    // Act
    var result = await _accountService.GetUserProfileAsync(userId);

    // Assert
    Assert.Null(result);
}
```

**檔案**: `Tests/Integration/AccountControllerTests.cs`

```csharp
[Fact]
public async Task GetMyProfile_WithValidToken_ReturnsUserProfile()
{
    // Arrange
    var token = await GetValidJwtToken(); // 輔助方法取得有效 token
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/account/me");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<ApiResponseModel<UserProfileResponse>>(content);
    
    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.NotNull(result.Data);
    Assert.NotEmpty(result.Data.Username);
}

[Fact]
public async Task GetMyProfile_WithoutToken_ReturnsUnauthorized()
{
    // Act
    var response = await _client.GetAsync("/api/account/me");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

---

## 測試流程

### 1. 執行單元測試
```bash
dotnet test --filter "FullyQualifiedName~AccountServiceTests"
```

### 2. 執行整合測試
```bash
dotnet test --filter "FullyQualifiedName~AccountControllerTests"
```

### 3. 手動 API 測試

**取得 JWT Token**:
```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "your_password"
}
```

**查詢個人資料**:
```bash
GET http://localhost:5000/api/account/me
Authorization: Bearer {your_jwt_token}
```

**預期回應** (200 OK):
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": {
    "username": "admin",
    "displayName": "Administrator",
    "roles": ["Admin"]
  },
  "timestamp": "2025-11-12T10:30:00Z",
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## 常見問題

### Q1: 如何處理沒有任何角色的用戶？
**A**: 回傳空陣列 `"roles": []`，而非 `null`

### Q2: DisplayName 為空時應該回傳什麼？
**A**: 回傳 `null`，遵循規格要求

### Q3: 停用帳號如何處理？
**A**: 在 JWT 驗證中介層階段就應該拒絕，回傳 401 Unauthorized

### Q4: 需要記錄稽核日誌嗎？
**A**: 不需要，規格明確要求不記錄以避免過多記錄

### Q5: 權限不足時回傳什麼？
**A**: 403 Forbidden + FORBIDDEN 業務代碼

---

## 檢查清單

開發完成前請確認：

- [ ] 權限定義已新增到 `seed_permissions.sql`
- [ ] UserProfileResponse DTO 已建立
- [ ] Repository 方法已實作（如需要）
- [ ] Service 方法已實作
- [ ] Controller 端點已新增並標註 `[RequirePermission]`
- [ ] 單元測試已撰寫並通過
- [ ] 整合測試已撰寫並通過
- [ ] API 文件已更新（Swagger/OpenAPI）
- [ ] 手動測試成功場景
- [ ] 手動測試失敗場景（401, 403, 404）
- [ ] 程式碼已遵循 Constitution 規範
- [ ] XML 註解已完成（繁體中文）

---

## 下一步

完成此功能後：
1. 執行完整測試套件
2. 建立 Pull Request
3. 等待 Code Review
4. 合併到主分支

## 參考資料

- [功能規格](./spec.md)
- [實作計劃](./plan.md)
- [資料模型](./data-model.md)
- [API 合約](./contracts/user-profile-api.yaml)
- [研究文件](./research.md)
