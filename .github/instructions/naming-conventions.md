# 命名規則 (Naming Conventions)

**專案**: V3.Admin.Backend  
**版本**: 1.0.0  
**最後更新**: 2025-10-26

## 概述

本文件定義專案中所有程式碼的命名規則,確保團隊成員使用一致的命名風格。

---

## C# 命名規則

### 1. 資料模型命名

#### 1.1 Entity (實體)

**定義**: 單一資料表的直接映射,與資料庫表格結構一對一對應。

**命名規則**:
- 使用名詞單數形式
- PascalCase 命名
- 不加後綴 (直接使用名詞)

**範例**:
```csharp
public class User { }          // ✅ 正確
public class Role { }          // ✅ 正確
public class Permission { }    // ✅ 正確

public class UserEntity { }    // ❌ 錯誤 (不需要 Entity 後綴)
public class Users { }         // ❌ 錯誤 (使用單數)
```

**使用場景**:
- Repository 層與資料庫互動
- 直接映射資料表欄位
- 包含所有資料庫欄位 (包含內部欄位如 Version, IsDeleted)

---

#### 1.2 Request (請求模型)

**定義**: API 層 (Controller) 的輸入模型,用於接收客戶端 HTTP 請求資料。

**命名規則**:
- 使用動詞或名詞 + `Request` 後綴
- PascalCase 命名
- 描述性命名,表明請求用途

**範例**:
```csharp
public class LoginRequest { }              // ✅ 正確
public class CreateAccountRequest { }      // ✅ 正確
public class UpdateAccountRequest { }      // ✅ 正確
public class ChangePasswordRequest { }     // ✅ 正確
public class DeleteAccountRequest { }      // ✅ 正確

public class LoginDto { }                  // ❌ 錯誤 (API 層使用 Request 而非 Dto)
public class AccountCreateRequest { }      // ❌ 錯誤 (動詞在前)
public class Login { }                     // ❌ 錯誤 (缺少 Request 後綴)
```

**使用場景**:
- **Controller 層接收 HTTP 請求**
- FluentValidation 驗證輸入
- Controller 將 Request 轉換為 Dto 傳給 Service

---

#### 1.3 Response (回應模型)

**定義**: API 層 (Controller) 的輸出模型,用於回傳資料給客戶端。

**命名規則**:
- 使用名詞 + `Response` 後綴
- PascalCase 命名
- 描述回應的資料內容

**範例**:
```csharp
public class LoginResponse { }             // ✅ 正確
public class AccountResponse { }           // ✅ 正確
public class AccountListResponse { }       // ✅ 正確
public class UserDetailResponse { }        // ✅ 正確

public class LoginDto { }                  // ❌ 錯誤 (API 層使用 Response 而非 Dto)
public class Account { }                   // ❌ 錯誤 (缺少 Response 後綴,易與 Entity 混淆)
public class ResponseLogin { }             // ❌ 錯誤 (Response 應為後綴)
```

**使用場景**:
- **Service 層將 Dto 轉換為 Response 回傳給 Controller**
- Controller 層包裝為 ApiResponseModel 回傳
- 不包含敏感資訊 (密碼雜湊、內部欄位)

---

#### 1.4 Dto (資料傳輸物件)

**定義**: Service 層的資料傳輸物件,用於 Service 層內部邏輯與跨層傳遞。

**命名規則**:
- 使用名詞 + `Dto` 後綴
- PascalCase 命名
- 描述資料用途或來源

**範例**:
```csharp
public class LoginDto { }                  // ✅ 正確
public class CreateAccountDto { }          // ✅ 正確
public class UpdateAccountDto { }          // ✅ 正確
public class AccountDto { }                // ✅ 正確
public class UserDto { }                   // ✅ 正確

public class LoginRequest { }              // ❌ 錯誤 (Service 層使用 Dto 而非 Request)
public class Login { }                     // ❌ 錯誤 (缺少 Dto 後綴)
public class DtoLogin { }                  // ❌ 錯誤 (Dto 應為後綴)
```

**使用場景**:
- **Service 層接收 Dto 作為方法參數**
- **Service 層返回 Dto 作為方法結果**
- Service 層將 Entity 轉換為 Dto
- Service 層將 Dto 轉換為 Entity
- 業務邏輯層的資料傳遞

**DTO 轉換流向**:
```
Controller 接收 Request → 轉換為 Dto → Service 處理
Service 返回 Dto → 轉換為 Response → Controller 回傳
```

---

#### 1.5 View (視圖模型)

**定義**: Join 查詢結果的複合模型,包含多表關聯資料。

**命名規則**:
- 使用描述性名詞 + `View` 後綴
- PascalCase 命名
- 表明包含哪些表的資料

**範例**:
```csharp
public class UserRoleView { }              // ✅ 正確 (User + Role)
public class AccountDetailView { }         // ✅ 正確 (Account 詳細資訊)
public class OrderItemView { }             // ✅ 正確 (Order + Item)
public class UserPermissionView { }        // ✅ 正確 (User + Permission)

public class UserRole { }                  // ❌ 錯誤 (缺少 View 後綴,易與 Entity 混淆)
public class ViewUserRole { }              // ❌ 錯誤 (View 應為後綴)
public class UserRoleDto { }               // ❌ 錯誤 (使用 View 而非 Dto)
```

**使用場景**:
- Repository 層執行 Join 查詢直接返回 View
- Service 層使用 View 無需額外轉換
- 包含跨表的複合資料

**何時使用 View**:
- ✅ 多表 Join 查詢 (SELECT ... FROM A JOIN B JOIN C)
- ✅ 需要聚合多個表的資料
- ❌ 單一表查詢 (使用 Entity → Response 轉換)
- ❌ 簡單的一對多關聯 (使用巢狀 Response)

---

### 2. 檔案與資料夾結構

```
Models/
├── Entities/              # Entity 類別 (資料庫映射)
│   ├── User.cs
│   ├── Role.cs
│   └── Permission.cs
├── Requests/              # Request 類別 (API 層輸入)
│   ├── LoginRequest.cs
│   ├── CreateAccountRequest.cs
│   └── UpdateAccountRequest.cs
├── Responses/             # Response 類別 (API 層輸出)
│   ├── LoginResponse.cs
│   ├── AccountResponse.cs
│   └── AccountListResponse.cs
├── Dtos/                  # Dto 類別 (Service 層傳輸)
│   ├── LoginDto.cs
│   ├── CreateAccountDto.cs
│   ├── UpdateAccountDto.cs
│   └── AccountDto.cs
├── Views/                 # View 類別 (Join 查詢結果)
│   ├── UserRoleView.cs
│   └── AccountDetailView.cs
└── ApiResponseModel.cs    # 統一 API 回應包裝
```

---

### 3. 分層使用規則

| 層級 | 使用模型 | 說明 |
|-----|---------|------|
| **Controller** | Request, Response | 接收 Request,轉換為 Dto 傳給 Service;<br>Service 返回 Dto,轉換為 Response 包裝於 ApiResponseModel |
| **Service** | Dto, Entity, View | 接收 Dto 作為參數,返回 Dto 作為結果;<br>Dto ↔ Entity/View 轉換在 Service 層完成 |
| **Repository** | Entity, View | 返回 Entity (單表) 或 View (Join 查詢) |

**資料流向**:
```
Client 
  ↓ (Request)
Controller 
  ↓ (Request → Dto 轉換)
Service 
  ↓ (Dto → Entity 轉換)
Repository 
  ↓ (SQL)
Database
  ↑ (Entity/View)
Repository
  ↑ (Entity/View)
Service
  ↑ (Entity/View → Dto 轉換)
Controller
  ↑ (Dto → Response 轉換, 包裝於 ApiResponseModel)
Client
```

**轉換範例**:
```csharp
// Controller 層
[HttpPost("login")]
public async Task<ActionResult<ApiResponseModel<LoginResponse>>> Login(LoginRequest request)
{
    // Request → Dto
    var loginDto = new LoginDto 
    { 
        Username = request.Username, 
        Password = request.Password 
    };
    
    // 呼叫 Service
    var resultDto = await _authService.LoginAsync(loginDto);
    
    // Dto → Response
    var response = new LoginResponse 
    { 
        Token = resultDto.Token,
        ExpiresAt = resultDto.ExpiresAt,
        User = new AccountResponse { ... }
    };
    
    return Ok(ApiResponseModel<LoginResponse>.CreateSuccess(response));
}

// Service 層
public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
{
    // Dto → Entity 驗證
    var user = await _userRepository.GetByUsernameAsync(loginDto.Username);
    
    // 業務邏輯...
    
    // Entity → Dto
    return new LoginResultDto 
    { 
        Token = token,
        ExpiresAt = expiresAt,
        UserId = user.Id
    };
}
```

---

### 4. 其他 C# 命名規則

#### 4.1 介面

**規則**: `I` 前綴 + PascalCase

```csharp
public interface IUserRepository { }       // ✅ 正確
public interface IAuthService { }          // ✅ 正確

public interface UserRepository { }        // ❌ 錯誤 (缺少 I 前綴)
```

---

#### 4.2 服務類別

**規則**: 名詞 + `Service` 後綴

```csharp
public class AuthService { }               // ✅ 正確
public class AccountService { }            // ✅ 正確

public class Auth { }                      // ❌ 錯誤 (缺少 Service 後綴)
```

---

#### 4.3 Repository 類別

**規則**: 名詞 + `Repository` 後綴

```csharp
public class UserRepository { }            // ✅ 正確
public class RoleRepository { }            // ✅ 正確

public class UserRepo { }                  // ❌ 錯誤 (不使用縮寫)
```

---

#### 4.4 Controller 類別

**規則**: 名詞 + `Controller` 後綴

```csharp
public class AuthController { }            // ✅ 正確
public class AccountController { }         // ✅ 正確

public class Auth { }                      // ❌ 錯誤 (缺少 Controller 後綴)
```

---

#### 4.5 驗證器類別

**規則**: DTO 名稱 + `Validator` 後綴

```csharp
public class LoginRequestValidator { }     // ✅ 正確
public class CreateAccountRequestValidator { } // ✅ 正確

public class LoginValidator { }            // ❌ 錯誤 (應包含完整 DTO 名稱)
```

---

### 5. 方法命名

**規則**: 動詞 + 名詞,使用 PascalCase

```csharp
public async Task<User> GetUserByIdAsync(Guid id) { }         // ✅ 正確
public async Task<bool> CreateAccountAsync(User user) { }     // ✅ 正確
public async Task UpdateDisplayNameAsync(Guid id, string name) { } // ✅ 正確

public async Task User(Guid id) { }                           // ❌ 錯誤 (缺少動詞)
public async Task get_user(Guid id) { }                       // ❌ 錯誤 (使用 snake_case)
```

---

### 6. 屬性與欄位命名

**規則**:
- **公開屬性**: PascalCase
- **私有欄位**: camelCase,可選 `_` 前綴

```csharp
public class User
{
    // 公開屬性
    public Guid Id { get; set; }               // ✅ 正確
    public string Username { get; set; }       // ✅ 正確
    
    // 私有欄位
    private readonly ILogger _logger;          // ✅ 正確 (底線前綴)
    private string username;                   // ✅ 正確 (無底線)
    
    private string UserName;                   // ❌ 錯誤 (私有欄位不使用 PascalCase)
}
```

---

### 7. 常數與列舉

**規則**:
- **常數**: PascalCase
- **列舉**: PascalCase (類型與值)

```csharp
public static class ResponseCodes
{
    public const string Success = "SUCCESS";           // ✅ 正確
    public const string ValidationError = "VALIDATION_ERROR"; // ✅ 正確
    
    public const string success = "SUCCESS";           // ❌ 錯誤 (使用 PascalCase)
}

public enum UserStatus
{
    Active,                                             // ✅ 正確
    Inactive,                                           // ✅ 正確
    Deleted                                             // ✅ 正確
    
    // active                                           // ❌ 錯誤 (使用 PascalCase)
}
```

---

## 資料庫命名規則

### 1. 資料表

**規則**: 小寫 + 底線分隔 (snake_case),使用複數形式

```sql
users                  -- ✅ 正確
user_roles             -- ✅ 正確
audit_logs             -- ✅ 正確

Users                  -- ❌ 錯誤 (使用小寫)
user                   -- ❌ 錯誤 (使用複數)
userRoles              -- ❌ 錯誤 (使用 snake_case)
```

---

### 2. 欄位

**規則**: 小寫 + 底線分隔 (snake_case)

```sql
id                     -- ✅ 正確
username               -- ✅ 正確
password_hash          -- ✅ 正確
created_at             -- ✅ 正確

Id                     -- ❌ 錯誤 (使用小寫)
userName               -- ❌ 錯誤 (使用 snake_case)
```

---

### 3. 索引

**規則**: `idx_` 前綴 + 表名 + 欄位名

```sql
idx_users_username                    -- ✅ 正確
idx_users_email                       -- ✅ 正確
idx_user_roles_user_id_role_id        -- ✅ 正確

users_username_idx                    -- ❌ 錯誤 (idx 應為前綴)
idx_username                          -- ❌ 錯誤 (應包含表名)
```

---

### 4. 外鍵

**規則**: `fk_` 前綴 + 表名 + 欄位名

```sql
fk_user_roles_user_id                 -- ✅ 正確
fk_audit_logs_user_id                 -- ✅ 正確

user_id_fk                            -- ❌ 錯誤 (fk 應為前綴)
fk_user_id                            -- ❌ 錯誤 (應包含表名)
```

---

## 檔案命名規則

### 1. C# 檔案

**規則**: 與類別名稱相同,PascalCase

```
User.cs                                // ✅ 正確
LoginRequest.cs                        // ✅ 正確
IUserRepository.cs                     // ✅ 正確

user.cs                                // ❌ 錯誤
login-request.cs                       // ❌ 錯誤
```

---

### 2. SQL 檔案

**規則**: 小寫 + 連字符 (kebab-case) 或底線 (snake_case)

```
001_create_users_table.sql             // ✅ 正確
002_add_indexes.sql                    // ✅ 正確
seed.sql                               // ✅ 正確

CreateUsersTable.sql                   // ❌ 錯誤
001-CreateUsersTable.sql               // ❌ 錯誤
```

---

## 總結

| 類型 | 命名規則 | 使用層級 | 範例 |
|-----|---------|---------|------|
| **Entity** | PascalCase, 名詞單數 | Repository | `User`, `Role` |
| **Request** | PascalCase, 動詞/名詞 + `Request` | Controller (輸入) | `LoginRequest`, `CreateAccountRequest` |
| **Response** | PascalCase, 名詞 + `Response` | Controller (輸出) | `AccountResponse`, `LoginResponse` |
| **Dto** | PascalCase, 名詞 + `Dto` | Service | `LoginDto`, `AccountDto`, `CreateAccountDto` |
| **View** | PascalCase, 名詞 + `View` | Repository, Service | `UserRoleView`, `AccountDetailView` |
| **Interface** | `I` + PascalCase | 所有層 | `IUserRepository`, `IAuthService` |
| **Service** | PascalCase, 名詞 + `Service` | Service | `AuthService`, `AccountService` |
| **Repository** | PascalCase, 名詞 + `Repository` | Repository | `UserRepository` |
| **Controller** | PascalCase, 名詞 + `Controller` | Controller | `AuthController` |
| **資料表** | snake_case, 複數 | Database | `users`, `user_roles` |
| **欄位** | snake_case | Database | `username`, `created_at` |
| **索引** | `idx_` + snake_case | Database | `idx_users_username` |

**分層資料流**:
```
Controller: Request → Dto (轉換)
Service:    Dto → Entity (轉換)
Repository: Entity/View (資料庫操作)
Repository: Entity/View ← Database
Service:    Entity/View → Dto (轉換)
Controller: Dto → Response (轉換, 包裝於 ApiResponseModel)
```

---

**版本**: 1.0.0  
**最後更新**: 2025-10-26  
**適用專案**: V3.Admin.Backend
