````markdown
# Quickstart 快速開始指南：權限模型重構

**Date**: 2025-11-16  
**Branch**: `004-permission-refactor`  
**Status**: Phase 1

---

## 概述

本指南協助開發者快速理解和使用重構後的權限模型。重點是 RoutePath 移除後的權限驗證方式，以及新增的 View 權限類型。

---

## 主要變更

### 1. RoutePath 完全移除

**以前**：
```csharp
// 舊系統中，Permission 實體包含 RoutePath
public class Permission
{
    public int Id { get; set; }
    public string PermissionCode { get; set; }
    public PermissionType PermissionType { get; set; }  // route, function
    public string RoutePath { get; set; }  // ❌ 已移除
}

// 權限驗證依賴 RoutePath
if (permission.RoutePath == "/api/inventory/create") { ... }
```

**現在**：
```csharp
// 新系統中，Permission 實體移除了 RoutePath
public class Permission
{
    public int Id { get; set; }
    public string PermissionCode { get; set; }  // ✅ 直接決定路由權限
    public PermissionType PermissionType { get; set; }  // function, view
    // ❌ RoutePath 已移除
}

// 權限驗證直接基於 PermissionCode
if (await _permissionValidationService.HasPermissionAsync(userId, "inventory.create")) { ... }
```

### 2. PermissionType 調整

**變更內容**：
- ❌ 移除 `route` 類型
- ✅ 保留 `function` 類型（操作權限）
- ✅ 新增 `view` 類型（UI 區塊瀏覽權限）

**應用場景**：

| 類型 | 用途 | 範例 | 前端用法 |
|------|------|------|---------|
| Function | 控制操作 | `inventory.create`, `role.delete` | 按鈕可見性 |
| View | 控制 UI 元件/區塊 | `dashboard.widget`, `reports.panel` | 區塊可見性 |

### 3. 權限代碼格式規範

**格式**：`resource.action` 或 `resource.subresource.action`

**範例**：

| 代碼 | 型別 | 說明 |
|------|------|------|
| `permission.create` | function | 建立新權限 |
| `permission.update` | function | 編輯權限 |
| `permission.delete` | function | 刪除權限 |
| `dashboard.summary_widget` | view | 儀表板摘要小工具 |
| `reports.analytics_panel` | view | 報表分析面板 |
| `inventory.warehouse.transfer` | function | 庫存倉庫轉移 |

---

## 後端實現步驟

### Step 1: 建立權限種子資料

在 `Database/Scripts/seed_permissions.sql` 中定義權限：

```sql
-- Function 類型權限
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('permission.create', '新增權限', '允許建立新的權限定義', 1),
    ('permission.read', '查詢權限', '允許查詢權限列表和詳情', 1),
    ('permission.update', '修改權限', '允許編輯權限資訊', 1),
    ('permission.delete', '刪除權限', '允許刪除權限', 1),
    ('role.assign_permission', '分配角色權限', '允許為角色分配權限', 1),
    
    -- View 類型權限
    ('dashboard.summary_widget', '摘要小工具', '允許查看儀表板摘要區塊', 2),
    ('reports.analytics_panel', '分析面板', '允許查看報表分析面板', 2)
ON CONFLICT (permission_code) DO NOTHING;
```

**說明**：
- `permission_type` 值：1 = Function, 2 = View
- 確保所有權限代碼唯一

### Step 2: 在控制器中應用 RequirePermission 屬性

```csharp
[ApiController]
[Route("api/[controller]")]
public class PermissionController : BaseApiController
{
    /// <summary>建立新權限</summary>
    [HttpPost]
    [RequirePermission("permission.create")]  // ✅ 指定所需權限
    public async Task<IActionResult> CreatePermission(CreatePermissionRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationError(ModelState);

        var result = await _permissionService.CreatePermissionAsync(request);
        return Created(result);
    }

    /// <summary>查詢權限列表</summary>
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

    /// <summary>編輯權限</summary>
    [HttpPut("{id}")]
    [RequirePermission("permission.update")]
    public async Task<IActionResult> UpdatePermission(int id, UpdatePermissionRequest request)
    {
        request.Id = id;
        var result = await _permissionService.UpdatePermissionAsync(request);
        return Success(result);
    }

    /// <summary>刪除權限</summary>
    [HttpDelete("{id}")]
    [RequirePermission("permission.delete")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        await _permissionService.DeletePermissionAsync(id);
        return Success(null, "權限已刪除", ResponseCodes.DELETED);
    }
}
```

### Step 3: 實現 PermissionValidationService

```csharp
public class PermissionValidationService : IPermissionValidationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionValidationService(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>檢查用戶是否擁有指定權限</summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        try
        {
            // 查詢用戶及其角色的所有權限
            var userPermissions = await _userRepository.GetUserPermissionsAsync(userId);
            
            // 檢查是否擁有指定的權限代碼
            return userPermissions.Any(p => 
                p.PermissionCode == permissionCode && !p.IsDeleted);
        }
        catch (Exception ex)
        {
            // 記錄異常並返回 false（安全失敗）
            return false;
        }
    }

    /// <summary>檢查用戶是否擁有指定型別的權限</summary>
    public async Task<bool> HasPermissionTypeAsync(int userId, string permissionCode, PermissionType type)
    {
        var permission = await _permissionRepository.GetByCodeAsync(permissionCode);
        if (permission == null || permission.IsDeleted)
            return false;

        if (permission.PermissionType != type)
            return false;

        return await HasPermissionAsync(userId, permissionCode);
    }

    /// <summary>驗證 PermissionCode 格式</summary>
    public bool ValidatePermissionCode(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return false;

        if (permissionCode.Length < 3 || permissionCode.Length > 100)
            return false;

        // 允許字母、數字、點號、下劃線
        var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9][a-zA-Z0-9._]{1,98}[a-zA-Z0-9]$|^[a-zA-Z0-9]$");
        return regex.IsMatch(permissionCode);
    }
}
```

### Step 4: 確認 PermissionAuthorizationMiddleware 配置

在 `Program.cs` 中確保中介軟體已正確註冊：

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// 註冊服務
builder.Services.AddScoped<IPermissionValidationService, PermissionValidationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
// ... 其他服務

var app = builder.Build();

// 使用中介軟體
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PermissionAuthorizationMiddleware>();  // ✅ 在授權後使用

app.MapControllers();
app.Run();
```

---

## 前端實現指南

### 1. 查詢用戶權限

```javascript
// 檢查用戶是否擁有特定權限
async function checkPermission(permissionCode) {
    const response = await fetch(`/api/permissions/check/${permissionCode}`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
    });
    
    const data = await response.json();
    return data.data.hasPermission;
}

// 使用範例
if (await checkPermission('inventory.create')) {
    // 顯示「建立」按鈕
} else {
    // 隱藏「建立」按鈕
}
```

### 2. 根據權限渲染 UI 元件

```vue
<template>
  <div>
    <!-- Function 型別權限 - 控制按鈕顯示 -->
    <button v-if="hasPermission('inventory.create')" @click="createItem">
      新增庫存
    </button>
    
    <!-- View 型別權限 - 控制區塊顯示 -->
    <div v-if="hasPermission('dashboard.summary_widget')" class="widget">
      <h3>摘要面板</h3>
      <p>顯示銷售摘要資訊</p>
    </div>
    
    <!-- 報表分析面板 -->
    <div v-if="hasPermission('reports.analytics_panel')" class="analytics">
      <h3>分析面板</h3>
      <!-- 圖表內容 -->
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const permissions = ref(new Set())

// 批量檢查權限
async function checkPermissions(codes) {
    for (const code of codes) {
        const hasIt = await checkPermission(code)
        if (hasIt) {
            permissions.value.add(code)
        }
    }
}

// 檢查單個權限
function hasPermission(code) {
    return permissions.value.has(code)
}

onMounted(async () => {
    // 初始化時檢查所有需要的權限
    await checkPermissions([
        'inventory.create',
        'dashboard.summary_widget',
        'reports.analytics_panel'
    ])
})
</script>
```

### 3. 權限檢查掛鉤（自訂指令）

```javascript
// 定義 v-permission 指令
app.directive('permission', {
    mounted(el, binding, vnode) {
        const permissionCode = binding.value
        
        checkPermission(permissionCode).then(has => {
            if (!has) {
                el.style.display = 'none'
            }
        })
    }
})

// 使用
<button v-permission="'inventory.create'">
    新增庫存
</button>
```

---

## 資料庫遷移步驟

### 1. 建立遷移文件

```bash
# 使用 EF Core 建立遷移
dotnet ef migrations add RemoveRoutePath -p V3.Admin.Backend.csproj
```

### 2. 遷移內容

遷移應包含：
- 刪除 `permissions` 表的 `route_path` 欄位
- 確保新的 CHECK 約束已套用
- 保留所有其他欄位和約束

```csharp
public override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "route_path",
        table: "permissions");
}

public override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "route_path",
        table: "permissions",
        type: "character varying(255)",
        nullable: true);
}
```

### 3. 執行遷移

```bash
# 更新開發環境資料庫
dotnet ef database update -p V3.Admin.Backend.csproj

# 生成遷移腳本供生產環境使用
dotnet ef migrations script -p V3.Admin.Backend.csproj -o Migrations/script.sql
```

---

## 測試檢查清單

### 後端測試

- [ ] **單元測試**：PermissionService 的建立、編輯、刪除邏輯
- [ ] **單元測試**：PermissionValidationService 的權限檢查
- [ ] **驗證測試**：PermissionCode 格式驗證
- [ ] **整合測試**：Permission API 端點（POST, GET, PUT, DELETE）
- [ ] **中介軟體測試**：PermissionAuthorizationMiddleware 的授權檢查
- [ ] **邊界情況**：刪除權限時的級聯檢查、並發更新

### 前端測試

- [ ] 檢查權限 API 呼叫成功
- [ ] UI 元件根據權限正確顯示/隱藏
- [ ] 無權限時隱藏敏感按鈕
- [ ] 異常情況下的降級處理

### 資料庫測試

- [ ] 遷移前後資料完整性
- [ ] RoutePath 欄位成功移除
- [ ] 新的 CHECK 約束生效
- [ ] 外鍵約束正確

---

## 常見問題 (FAQ)

**Q: 如何處理舊系統中的 RoutePath 資料？**  
A: 當前系統無舊 RoutePath 資料，直接移除。若有歷史資料，需手動遷移至新的權限代碼。

**Q: 能否在運行時新增新的 PermissionType？**  
A: 短期內不支持（使用 Enum）。若未來需要，可遷移至資料庫表 (permission_types)，參見 data-model.md。

**Q: 權限檢查的性能如何？**  
A: 應維持在 50ms 內。建議在 PermissionValidationService 中實現快取機制。

**Q: 前端如何快速渲染，而不必逐個查詢權限？**  
A: 建議在登入後一次性查詢用戶所有權限，存儲在本地或 Vuex/Pinia 狀態管理中。

---

## 相關文件

- **Specification**: `/specs/004-permission-refactor/spec.md` - 完整需求說明
- **Research**: `/specs/004-permission-refactor/research.md` - 設計決策背景
- **Data Model**: `/specs/004-permission-refactor/data-model.md` - 詳細實體設計
- **API Contract**: `/specs/004-permission-refactor/contracts/permission-api.yaml` - OpenAPI 規範
- **Tasks**: `/specs/004-permission-refactor/tasks.md` - 實施任務清單（Phase 2）

````
