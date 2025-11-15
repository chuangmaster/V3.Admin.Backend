````markdown
# Implementation Tasks 實施任務清單：權限模型重構

**Date**: 2025-11-16  
**Branch**: `004-permission-refactor`  
**Status**: Phase 2 - Implementation Planning

---

## 概述

本文件列出實施「權限模型重構 - 移除 RoutePath、整合路由決策至 PermissionCode」所需的所有任務。任務按照實施順序分為多個階段，每個任務包含具體的驗收標準和優先級。

---

## 任務優先級說明

- **P0 (Critical)**: 核心功能，必須完成
- **P1 (High)**: 重要功能，應盡快完成
- **P2 (Medium)**: 增強功能，可適度延後
- **P3 (Low)**: 優化項目，非必需

---

## Phase 2.1: 資料模型更新

### Task 2.1.1: 建立 EF Core 遷移文件 [P0]

**目的**: 從資料庫中移除 `route_path` 欄位

**實施步驟**:

```bash
# 生成遷移
dotnet ef migrations add RemoveRoutePath -p V3.Admin.Backend.csproj -o Database/Migrations

# 驗證遷移內容
cat Database/Migrations/*_RemoveRoutePath.cs
```

**驗收標準**:
- [ ] 遷移文件正確生成於 `Database/Migrations/` 目錄
- [ ] `Up()` 方法包含 `DropColumn("route_path", "permissions")`
- [ ] `Down()` 方法能正確回滾變更
- [ ] 遷移檔名包含時間戳

**依賴**: 無

**工作量估計**: 0.5 小時

---

### Task 2.1.2: 從 Permission 實體移除 RoutePath 屬性 [P0]

**目的**: 更新 C# 實體類，移除已廢棄的欄位

**檔案**: `Models/Entities/Permission.cs`

**實施步驟**:

1. 開啟 `Models/Entities/Permission.cs`
2. 移除以下屬性:
   ```csharp
   public string RoutePath { get; set; }  // ❌ 移除此行
   ```
3. 保留所有其他屬性（Id, PermissionCode, Name, Description, PermissionType, IsDeleted 等）
4. 確保 Version 屬性存在（用於樂觀並發控制）

**變更範例**:

```csharp
// 移除前
public class Permission
{
    public int Id { get; set; }
    public string PermissionCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PermissionType PermissionType { get; set; }
    public string RoutePath { get; set; }  // ❌ 此行需移除
    public bool IsDeleted { get; set; }
    // ... 其他屬性
}

// 移除後
public class Permission
{
    public int Id { get; set; }
    public string PermissionCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PermissionType PermissionType { get; set; }
    // ❌ RoutePath 已移除
    public bool IsDeleted { get; set; }
    // ... 其他屬性
}
```

**驗收標準**:
- [ ] RoutePath 屬性完全移除
- [ ] 所有其他屬性保持不變
- [ ] 類別編譯無誤
- [ ] XML 文件註解正確

**依賴**: Task 2.1.1

**工作量估計**: 0.5 小時

---

### Task 2.1.3: 新增 PermissionType Enum (如不存在) [P0]

**目的**: 確保 PermissionType 列舉正確定義

**檔案**: `Models/Entities/PermissionType.cs` (新建或更新)

**實施步驟**:

```csharp
/// <summary>
/// 權限類型列舉
/// </summary>
public enum PermissionType
{
    /// <summary>功能操作權限</summary>
    Function = 1,
    
    /// <summary>UI 區塊瀏覽權限</summary>
    View = 2
}
```

**驗收標準**:
- [ ] Enum 定義包含 Function 和 View 兩種類型
- [ ] 值分別為 1 和 2
- [ ] XML 文件註解完整
- [ ] 無編譯錯誤

**依賴**: 無

**工作量估計**: 0.25 小時

---

## Phase 2.2: 資料存取層更新

### Task 2.2.1: 更新 PermissionRepository [P0]

**目的**: 調整資料庫查詢邏輯以適應新模型

**檔案**: `Repositories/PermissionRepository.cs`

**實施步驟**:

1. **移除 RoutePath 相關查詢**:
   - 刪除任何包含 `route_path` 欄位的 SELECT 語句
   - 刪除任何按 RoutePath 篩選的查詢

2. **新增/更新以下方法** (如不存在):
   ```csharp
   /// <summary>根據權限代碼查詢權限</summary>
   public async Task<Permission> GetByCodeAsync(string permissionCode)
   {
       const string sql = @"
           SELECT id, permission_code, name, description, permission_type, 
                  is_deleted, created_by, created_at, updated_by, updated_at, 
                  deleted_by, deleted_at, version
           FROM permissions
           WHERE permission_code = @PermissionCode
       ";
       
       using (var connection = new NpgsqlConnection(_connectionString))
       {
           var result = await connection.QueryFirstOrDefaultAsync<Permission>(
               sql,
               new { PermissionCode = permissionCode });
           return result;
       }
   }

   /// <summary>查詢指定型別的所有權限</summary>
   public async Task<IEnumerable<Permission>> GetByTypeAsync(PermissionType permissionType)
   {
       const string sql = @"
           SELECT id, permission_code, name, description, permission_type, 
                  is_deleted, created_by, created_at, updated_by, updated_at, 
                  deleted_by, deleted_at, version
           FROM permissions
           WHERE permission_type = @PermissionType AND is_deleted = false
           ORDER BY permission_code
       ";
       
       using (var connection = new NpgsqlConnection(_connectionString))
       {
           var results = await connection.QueryAsync<Permission>(
               sql,
               new { PermissionType = (int)permissionType });
           return results;
       }
   }

   /// <summary>驗證 PermissionCode 是否唯一</summary>
   public async Task<bool> IsCodeUniqueAsync(string permissionCode, int? excludeId = null)
   {
       const string sql = @"
           SELECT COUNT(*) FROM permissions
           WHERE permission_code = @PermissionCode
           AND is_deleted = false
           AND (@ExcludeId IS NULL OR id != @ExcludeId)
       ";
       
       using (var connection = new NpgsqlConnection(_connectionString))
       {
           var count = await connection.ExecuteScalarAsync<int>(
               sql,
               new { PermissionCode = permissionCode, ExcludeId = excludeId });
           return count == 0;
       }
   }
   ```

3. **更新 INSERT 和 UPDATE SQL**:
   - 確保不包含 `route_path` 欄位
   - 驗證所有欄位對應正確

**驗收標準**:
- [ ] RoutePath 相關查詢完全移除
- [ ] 新方法正確實現
- [ ] SQL 參數化以防 SQL 注入
- [ ] 使用 Dapper ORM 正確映射
- [ ] 所有方法有 XML 文件註解

**依賴**: Task 2.1.2

**工作量估計**: 2 小時

---

### Task 2.2.2: 更新 IPermissionRepository 介面 [P0]

**目的**: 新增或移除相應的介面方法

**檔案**: `Repositories/Interfaces/IPermissionRepository.cs`

**實施步驟**:

```csharp
public interface IPermissionRepository
{
    // 現有方法
    Task<Permission> GetByIdAsync(int id);
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<Permission> CreateAsync(Permission permission);
    Task UpdateAsync(Permission permission);
    Task DeleteAsync(int id);
    
    // 新增方法
    Task<Permission> GetByCodeAsync(string permissionCode);
    Task<IEnumerable<Permission>> GetByTypeAsync(PermissionType permissionType);
    Task<bool> IsCodeUniqueAsync(string permissionCode, int? excludeId = null);
    
    // ❌ 移除以下方法（如存在）
    // Task<Permission> GetByRoutePathAsync(string routePath);  // ← 移除
    // Task<IEnumerable<Permission>> GetByTypeAndRouteAsync(...);  // ← 移除
}
```

**驗收標準**:
- [ ] 介面定義包含所有必需方法
- [ ] 方法簽名與實現一致
- [ ] RoutePath 相關方法完全移除
- [ ] XML 文件註解完整

**依賴**: Task 2.2.1

**工作量估計**: 0.5 小時

---

## Phase 2.3: 業務邏輯層更新

### Task 2.3.1: 實現 PermissionValidationService [P0]

**目的**: 權限驗證核心服務，用於檢查用戶是否擁有權限

**檔案**: `Services/PermissionValidationService.cs`

**實施步驟**:

```csharp
public class PermissionValidationService : IPermissionValidationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionValidationService> _logger;

    public PermissionValidationService(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        ILogger<PermissionValidationService> logger)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    /// <summary>檢查用戶是否擁有指定權限</summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        try
        {
            // 驗證輸入
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            // 查詢用戶的所有權限
            var userPermissions = await _userRepository.GetUserPermissionsAsync(userId);
            
            // 檢查是否擁有指定的權限
            var hasPermission = userPermissions.Any(p => 
                p.PermissionCode == permissionCode && !p.IsDeleted);
            
            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}, code {PermissionCode}", 
                userId, permissionCode);
            return false;  // 安全失敗
        }
    }

    /// <summary>檢查用戶是否擁有指定型別的權限</summary>
    public async Task<bool> HasPermissionTypeAsync(int userId, string permissionCode, PermissionType type)
    {
        try
        {
            // 查詢權限定義
            var permission = await _permissionRepository.GetByCodeAsync(permissionCode);
            if (permission == null || permission.IsDeleted)
                return false;

            // 驗證型別
            if (permission.PermissionType != type)
                return false;

            // 檢查用戶是否擁有此權限
            return await HasPermissionAsync(userId, permissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission type for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>驗證 PermissionCode 格式</summary>
    public bool ValidatePermissionCode(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return false;

        if (permissionCode.Length < 3 || permissionCode.Length > 100)
            return false;

        // 正則驗證：允許字母、數字、點號、下劃線
        var regex = new System.Text.RegularExpressions.Regex(
            @"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$");
        
        return regex.IsMatch(permissionCode);
    }
}
```

**驗收標準**:
- [ ] 方法實現正確且符合規範
- [ ] 錯誤處理適當（例外不拋出，返回 false）
- [ ] 日誌記錄完整
- [ ] XML 文件註解齊全
- [ ] 單元測試覆蓋核心邏輯

**依賴**: Task 2.2.1, 2.2.2

**工作量估計**: 2 小時

---

### Task 2.3.2: 實現 IPermissionValidationService 介面 [P0]

**檔案**: `Services/Interfaces/IPermissionValidationService.cs`

**實施步驟**:

```csharp
public interface IPermissionValidationService
{
    /// <summary>檢查用戶是否擁有指定權限</summary>
    Task<bool> HasPermissionAsync(int userId, string permissionCode);

    /// <summary>檢查用戶是否擁有指定型別的權限</summary>
    Task<bool> HasPermissionTypeAsync(int userId, string permissionCode, PermissionType type);

    /// <summary>驗證 PermissionCode 格式</summary>
    bool ValidatePermissionCode(string permissionCode);
}
```

**驗收標準**:
- [ ] 介面定義清晰
- [ ] 方法簽名與實現一致
- [ ] XML 文件註解完整

**依賴**: Task 2.3.1

**工作量估計**: 0.25 小時

---

### Task 2.3.3: 更新 PermissionService [P1]

**目的**: 更新權限業務邏輯，移除 RoutePath 相關邏輯

**檔案**: `Services/PermissionService.cs`

**實施步驟**:

1. **移除 RoutePath 相關邏輯**:
   - 刪除任何涉及 `RoutePath` 的驗證、轉換或映射代碼

2. **更新 CreatePermissionAsync**:
   ```csharp
   public async Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request)
   {
       // 驗證輸入
       if (!_permissionValidationService.ValidatePermissionCode(request.PermissionCode))
           throw new ArgumentException("權限代碼格式不正確");

       // 檢查唯一性
       if (!await _permissionRepository.IsCodeUniqueAsync(request.PermissionCode))
           throw new InvalidOperationException("權限代碼已存在");

       // 驗證 PermissionType
       var permissionType = ParsePermissionType(request.PermissionType);

       // 建立權限
       var permission = new Permission
       {
           PermissionCode = request.PermissionCode,
           Name = request.Name,
           Description = request.Description,
           PermissionType = permissionType,
           CreatedBy = _userContext.UserId,
           CreatedAt = DateTime.UtcNow,
           Version = 0
       };

       var created = await _permissionRepository.CreateAsync(permission);
       
       // 記錄稽核
       await _auditLogService.LogAsync("Permission", "Create", request.PermissionCode, _userContext.UserId);

       return MapToResponse(created);
   }
   ```

3. **更新 UpdatePermissionAsync**:
   ```csharp
   public async Task<PermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request)
   {
       // 查詢現有權限
       var permission = await _permissionRepository.GetByIdAsync(request.Id);
       if (permission == null)
           throw new NotFoundException("權限不存在");

       // 檢查版本（樂觀鎖）
       if (permission.Version != request.Version)
           throw new ConcurrentUpdateException("權限已被其他用戶更新");

       // 驗證 PermissionType
       var permissionType = ParsePermissionType(request.PermissionType);

       // 更新
       permission.Name = request.Name;
       permission.Description = request.Description;
       permission.PermissionType = permissionType;
       permission.UpdatedBy = _userContext.UserId;
       permission.UpdatedAt = DateTime.UtcNow;
       permission.Version++;

       await _permissionRepository.UpdateAsync(permission);
       
       // 記錄稽核
       await _auditLogService.LogAsync("Permission", "Update", permission.PermissionCode, _userContext.UserId);

       return MapToResponse(permission);
   }
   ```

4. **新增輔助方法**:
   ```csharp
   private PermissionType ParsePermissionType(string typeString)
   {
       if (!Enum.TryParse<PermissionType>(typeString, ignoreCase: true, out var result))
           throw new ArgumentException($"無效的權限類型: {typeString}");
       return result;
   }
   ```

**驗收標準**:
- [ ] RoutePath 邏輯完全移除
- [ ] 新方法實現正確
- [ ] 適當的例外處理
- [ ] 稽核日誌記錄完整
- [ ] 單元測試覆蓋

**依賴**: Task 2.3.1, 2.3.2

**工作量估計**: 3 小時

---

## Phase 2.4: 展示層更新

### Task 2.4.1: 更新 CreatePermissionRequest DTO [P0]

**目的**: 移除 RoutePath 欄位

**檔案**: `Models/Requests/CreatePermissionRequest.cs`

**實施步驟**:

```csharp
public class CreatePermissionRequest
{
    /// <summary>權限代碼，必填，遵循 resource.action 格式</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限名稱，必填</summary>
    public string Name { get; set; }

    /// <summary>權限描述</summary>
    public string Description { get; set; }

    /// <summary>權限類型，必填：function 或 view</summary>
    public string PermissionType { get; set; }

    // ❌ RoutePath 已移除
}
```

**驗收標準**:
- [ ] RoutePath 欄位移除
- [ ] 所有必填欄位保留
- [ ] XML 文件註解完整

**依賴**: 無

**工作量估計**: 0.25 小時

---

### Task 2.4.2: 更新 UpdatePermissionRequest DTO [P0]

**檔案**: `Models/Requests/UpdatePermissionRequest.cs`

**變更**: 移除 RoutePath 欄位（同 Task 2.4.1）

**工作量估計**: 0.25 小時

---

### Task 2.4.3: 更新 PermissionResponse DTO [P0]

**檔案**: `Models/Responses/PermissionResponse.cs`

**變更**: 移除 RoutePath 欄位

**工作量估計**: 0.25 小時

---

### Task 2.4.4: 新增 CheckPermissionResponse DTO [P0]

**目的**: 支持前端查詢用戶權限

**檔案**: `Models/Responses/CheckPermissionResponse.cs` (新建)

**實施步驟**:

```csharp
public class CheckPermissionResponse
{
    /// <summary>權限代碼</summary>
    public string PermissionCode { get; set; }

    /// <summary>權限類型</summary>
    public string PermissionType { get; set; }

    /// <summary>用戶是否擁有此權限</summary>
    public bool HasPermission { get; set; }
}
```

**驗收標準**:
- [ ] 類別正確定義
- [ ] XML 文件註解完整

**依賴**: 無

**工作量估計**: 0.25 小時

---

### Task 2.4.5: 更新 CreatePermissionRequestValidator [P1]

**目的**: 移除 RoutePath 驗證，新增 PermissionCode 和 PermissionType 驗證

**檔案**: `Validators/CreatePermissionRequestValidator.cs`

**實施步驟**:

```csharp
public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.PermissionCode)
            .NotEmpty().WithMessage("權限代碼不可為空")
            .Length(3, 100).WithMessage("權限代碼長度須為 3-100 字元")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$")
            .WithMessage("權限代碼只允許字母、數字、點號、下劃線");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("權限名稱不可為空")
            .Length(1, 255).WithMessage("權限名稱長度須為 1-255 字元");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("權限描述最多 500 字元");

        RuleFor(x => x.PermissionType)
            .NotEmpty().WithMessage("權限類型不可為空")
            .Must(x => x == "function" || x == "view")
            .WithMessage("權限類型只能是 'function' 或 'view'");

        // ❌ RoutePath 驗證已移除
    }
}
```

**驗收標準**:
- [ ] RoutePath 驗證完全移除
- [ ] 所有必填欄位驗證正確
- [ ] 驗證訊息清晰易懂

**依賴**: Task 2.4.1

**工作量估計**: 1 小時

---

### Task 2.4.6: 更新 UpdatePermissionRequestValidator [P1]

**變更**: 類似 Task 2.4.5，移除 RoutePath 驗證

**工作量估計**: 1 小時

---

### Task 2.4.7: 更新 PermissionController [P1]

**目的**: 新增權限檢查端點，更新現有端點

**檔案**: `Controllers/PermissionController.cs`

**實施步驟**:

```csharp
[ApiController]
[Route("api/[controller]")]
public class PermissionController : BaseApiController
{
    private readonly IPermissionService _permissionService;
    private readonly IPermissionValidationService _permissionValidationService;

    [HttpPost]
    [RequirePermission("permission.create")]
    public async Task<IActionResult> CreatePermission(CreatePermissionRequest request)
    {
        var result = await _permissionService.CreatePermissionAsync(request);
        return Created(result);
    }

    [HttpGet]
    [RequirePermission("permission.read")]
    public async Task<IActionResult> GetPermissions(
        int page = 1, 
        int pageSize = 20, 
        string permissionType = null,
        string keyword = null)
    {
        var result = await _permissionService.GetPermissionsAsync(page, pageSize, permissionType, keyword);
        return Success(result);
    }

    [HttpGet("{id}")]
    [RequirePermission("permission.read")]
    public async Task<IActionResult> GetPermission(int id)
    {
        var result = await _permissionService.GetPermissionAsync(id);
        return Success(result);
    }

    [HttpPut("{id}")]
    [RequirePermission("permission.update")]
    public async Task<IActionResult> UpdatePermission(int id, UpdatePermissionRequest request)
    {
        request.Id = id;
        var result = await _permissionService.UpdatePermissionAsync(request);
        return Success(result);
    }

    [HttpDelete("{id}")]
    [RequirePermission("permission.delete")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        await _permissionService.DeletePermissionAsync(id);
        return Success(null, "權限已刪除", ResponseCodes.DELETED);
    }

    /// <summary>檢查當前用戶是否擁有指定權限</summary>
    [HttpGet("check/{permissionCode}")]
    [Authorize]
    public async Task<IActionResult> CheckPermission(string permissionCode)
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
        if (userId <= 0)
            return Unauthorized();

        var hasPermission = await _permissionValidationService.HasPermissionAsync(userId, permissionCode);
        
        var response = new CheckPermissionResponse
        {
            PermissionCode = permissionCode,
            PermissionType = "unknown",
            HasPermission = hasPermission
        };

        return Success(response);
    }
}
```

**驗收標準**:
- [ ] 所有端點正確實現
- [ ] RequirePermission 屬性正確應用
- [ ] 新檢查權限端點實現
- [ ] 無編譯錯誤
- [ ] 端點測試通過

**依賴**: Task 2.4.4, 2.4.5, 2.4.6, 2.3.3

**工作量估計**: 2 小時

---

## Phase 2.5: 中介軟體與其他層更新

### Task 2.5.1: 確認 PermissionAuthorizationMiddleware 實現 [P1]

**目的**: 驗證中介軟體正確使用 PermissionCode（而非 RoutePath）

**檔案**: `Middleware/PermissionAuthorizationMiddleware.cs`

**驗收標準**:
- [ ] 中介軟體使用 PermissionCode 進行權限檢查
- [ ] 無 RoutePath 相關邏輯
- [ ] 權限檢查失敗時記錄 PermissionFailureLog
- [ ] 返回適當的 HTTP 狀態碼 (403)

**工作量估計**: 1 小時

---

### Task 2.5.2: 更新 PermissionFailureLog 記錄邏輯 [P1]

**目的**: 調整失敗日誌以適應新模型

**檔案**: `Repositories/PermissionFailureLogRepository.cs`, `Models/Entities/PermissionFailureLog.cs`

**實施步驟**:

1. 確保 PermissionFailureLog 實體不含 `route_path` 欄位
2. 記錄 `permission_code` 和 `request_path` 而非 `route_path`
3. 更新記錄邏輯

**驗收標準**:
- [ ] 日誌記錄正確的 PermissionCode
- [ ] 記錄實際請求路徑
- [ ] 無 RoutePath 相關欄位

**工作量估計**: 1 小時

---

## Phase 2.6: 資料庫種子資料

### Task 2.6.1: 建立/更新權限種子指令碼 [P0]

**目的**: 初始化資料庫中的權限資料

**檔案**: `Database/Scripts/seed_permissions.sql` (新建或更新)

**內容**:

```sql
-- 權限管理相關
INSERT INTO permissions (permission_code, name, description, permission_type, created_at)
VALUES 
    ('permission.create', '新增權限', '允許建立新的權限定義', 1, NOW()),
    ('permission.read', '查詢權限', '允許查詢權限列表和詳情', 1, NOW()),
    ('permission.update', '修改權限', '允許編輯權限資訊', 1, NOW()),
    ('permission.delete', '刪除權限', '允許刪除權限', 1, NOW()),
    
    -- 角色管理相關
    ('role.create', '新增角色', '允許建立新的角色', 1, NOW()),
    ('role.read', '查詢角色', '允許查詢角色列表', 1, NOW()),
    ('role.update', '修改角色', '允許編輯角色資訊', 1, NOW()),
    ('role.delete', '刪除角色', '允許刪除角色', 1, NOW()),
    ('role.assign_permission', '分配角色權限', '允許為角色分配權限', 1, NOW()),
    
    -- UI 區塊權限
    ('dashboard.summary_widget', '儀表板摘要', '允許查看儀表板摘要小工具', 2, NOW()),
    ('reports.analytics_panel', '分析面板', '允許查看報表分析面板', 2, NOW())
ON CONFLICT (permission_code) DO NOTHING;
```

**驗收標準**:
- [ ] 種子資料正確插入
- [ ] 所有權限代碼唯一
- [ ] PermissionType 值正確
- [ ] 無錯誤執行

**工作量估計**: 0.5 小時

---

## Phase 2.7: 測試

### Task 2.7.1: 單元測試 - PermissionValidationService [P1]

**檔案**: `Tests/Unit/Services/PermissionValidationServiceTests.cs`

**測試案例**:
- [ ] HasPermissionAsync - 用戶擁有權限時返回 true
- [ ] HasPermissionAsync - 用戶無權限時返回 false
- [ ] HasPermissionTypeAsync - 型別匹配時返回 true
- [ ] HasPermissionTypeAsync - 型別不匹配時返回 false
- [ ] ValidatePermissionCode - 有效代碼通過驗證
- [ ] ValidatePermissionCode - 無效代碼失敗驗證
- [ ] 例外情況的安全失敗

**工作量估計**: 3 小時

---

### Task 2.7.2: 整合測試 - Permission API [P1]

**檔案**: `Tests/Integration/Controllers/PermissionControllerTests.cs`

**測試案例**:
- [ ] POST /api/permissions - 建立權限成功
- [ ] POST /api/permissions - 重複 PermissionCode 失敗
- [ ] POST /api/permissions - 格式驗證失敗
- [ ] GET /api/permissions - 查詢列表成功
- [ ] GET /api/permissions/{id} - 查詢詳情成功
- [ ] PUT /api/permissions/{id} - 編輯成功
- [ ] PUT /api/permissions/{id} - 版本衝突返回 409
- [ ] DELETE /api/permissions/{id} - 刪除成功
- [ ] GET /api/permissions/check/{code} - 檢查權限成功
- [ ] 無權限用戶返回 403

**工作量估計**: 4 小時

---

### Task 2.7.3: 中介軟體測試 [P1]

**檔案**: `Tests/Integration/Middleware/PermissionAuthorizationMiddlewareTests.cs`

**測試案例**:
- [ ] 有權限用戶可訪問受保護端點
- [ ] 無權限用戶返回 403
- [ ] 權限失敗被正確記錄
- [ ] 無效 JWT 返回 401

**工作量估計**: 3 小時

---

### Task 2.7.4: 資料庫遷移測試 [P0]

**驗證步驟**:
- [ ] `RoutePath` 欄位成功移除
- [ ] 現有權限資料保持完整
- [ ] 新權限能正常建立
- [ ] 回滾遷移成功

**工作量估計**: 1 小時

---

## Phase 2.8: 文件與清理

### Task 2.8.1: 更新 README 和 API 文件 [P2]

**檔案**: `README.md`, `specs/V3.Admin.Backend.API.yaml`

**變更內容**:
- [ ] 移除 RoutePath 相關說明
- [ ] 新增 PermissionType 和 PermissionCode 說明
- [ ] 更新 API 合約文件

**工作量估計**: 1 小時

---

### Task 2.8.2: 代碼清理與檢查 [P2]

**步驟**:
- [ ] 移除所有被註解的 RoutePath 代碼
- [ ] 檢查是否有遺漏的 RoutePath 引用
- [ ] 執行靜態程式碼分析
- [ ] 修復所有編譯警告

**工作量估計**: 1 小時

---

### Task 2.8.3: 移除舊遷移或建立新遷移索引 [P3]

**目的**: 清理資料庫遷移歷史（可選）

**工作量估計**: 0.5 小時

---

## 總結

### 總體工作量估計

| Phase | 任務數 | 估計工時 |
|-------|-------|--------|
| 2.1 資料模型 | 3 | 1.25 h |
| 2.2 資料存取層 | 2 | 2.5 h |
| 2.3 業務邏輯層 | 3 | 5.25 h |
| 2.4 展示層 | 7 | 7.25 h |
| 2.5 中介軟體 | 2 | 2 h |
| 2.6 種子資料 | 1 | 0.5 h |
| 2.7 測試 | 4 | 11 h |
| 2.8 文件清理 | 3 | 2.5 h |
| **總計** | **25** | **~32 h** |

### 實施建議

1. **按順序實施**: 先完成 Phase 2.1-2.3，再進行 2.4-2.5
2. **並行測試**: 在實施過程中同步進行單元測試
3. **版本控制**: 使用功能分支 `004-permission-refactor`，定期推送進度
4. **代碼審查**: 每個 Phase 完成後進行 PR 審查
5. **文件同步**: 確保文件與代碼實現保持一致

### 驗收標準 (整體)

- [ ] 所有 25 個任務完成
- [ ] 單元測試覆蓋率 > 80%
- [ ] 整合測試全部通過
- [ ] 無編譯錯誤和警告
- [ ] RoutePath 相關代碼完全移除（grep 檢查）
- [ ] 資料庫遷移正確執行
- [ ] API 文件更新完整
- [ ] 代碼審查批准通過

---

## 附錄：驗收檢查清單

### 代碼完整性

```bash
# 檢查 RoutePath 引用
grep -r "RoutePath" --include="*.cs" .

# 檢查 route 型別引用
grep -r "PermissionType.Route" --include="*.cs" .

# 預期結果：無匹配項目
```

### 資料庫完整性

```sql
-- 驗證 RoutePath 欄位已移除
SELECT * FROM information_schema.columns 
WHERE table_name = 'permissions' AND column_name = 'route_path';

-- 預期結果：無行

-- 驗證權限資料完整
SELECT COUNT(*) FROM permissions WHERE is_deleted = false;

-- 驗證權限型別
SELECT DISTINCT permission_type FROM permissions;

-- 預期結果：1, 2 (Function, View)
```

````
