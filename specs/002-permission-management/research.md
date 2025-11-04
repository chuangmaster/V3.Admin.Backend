# 研究報告：權限管理機制技術決策

**Feature**: 002-permission-management  
**Date**: 2025-11-05  
**Purpose**: 解決 Technical Context 中的所有 NEEDS CLARIFICATION，提供技術選型、最佳實踐和解決方案

---

## 研究項目 1: 權限驗證性能優化（多角色權限合併）

### 問題描述
當用戶擁有多個角色時，系統需要合併所有角色的權限（聯集）來驗證用戶是否有權限執行特定操作。每次 API 請求都需要進行權限驗證，高頻操作可能成為性能瓶頸。目標是在 100ms 內完成權限驗證（包含多角色權限合併）。

### 研究發現

#### 方案 A: 即時查詢（無快取）
**實作方式**:
```sql
-- 每次驗證時執行此查詢
SELECT p.permission_code, p.permission_type
FROM permissions p
INNER JOIN role_permissions rp ON p.id = rp.permission_id
INNER JOIN user_roles ur ON rp.role_id = ur.role_id
WHERE ur.user_id = @userId 
  AND ur.is_deleted = false 
  AND p.is_deleted = false
  AND p.permission_code = @permissionCode;
```

**優點**:
- 權限變更即時生效（符合 FR-025 需求：下次請求時使用最新配置）
- 無需維護快取失效邏輯
- 實作簡單，無快取一致性問題

**缺點**:
- 每次請求都需要資料庫查詢
- 高併發時資料庫壓力較大

**性能評估**: 
- 使用適當索引（user_id, permission_code）後，單次查詢 <10ms
- 1000 TPS 併發時資料庫連接池可承受（PostgreSQL 連接池配置 100-200 connections）
- 符合 <100ms 的目標

#### 方案 B: 分散式快取（Redis）
**實作方式**:
```csharp
// 快取用戶的所有權限（聯集結果）
var cacheKey = $"user_permissions:{userId}";
var permissions = await _cache.GetAsync<List<string>>(cacheKey);
if (permissions == null) {
    permissions = await _repository.GetUserPermissionsAsync(userId);
    await _cache.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(15));
}
```

**優點**:
- 減少資料庫查詢頻率
- 讀取速度極快（<1ms）

**缺點**:
- 需要引入 Redis 依賴（增加系統複雜度）
- 快取失效邏輯複雜（權限變更、角色權限變更、用戶角色變更都需要清除快取）
- 與 FR-025「即時生效」需求衝突（15 分鐘快取會導致延遲）
- 分散式系統中快取一致性難以保證

#### 方案 C: 記憶體快取（IMemoryCache）
**實作方式**:
```csharp
var cacheKey = $"user_permissions:{userId}";
var permissions = await _memoryCache.GetOrCreateAsync(cacheKey, async entry => {
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await _repository.GetUserPermissionsAsync(userId);
});
```

**優點**:
- 無需外部依賴
- 讀取速度快（<1ms）

**缺點**:
- 單一伺服器實例有效（多實例部署時快取不一致）
- 快取失效邏輯複雜
- 與 FR-025「即時生效」需求衝突

### 決策: 方案 A（即時查詢，無快取）

**理由**:
1. **符合規格需求**: FR-025 明確要求「權限變更必須在下次請求時即時生效」，快取方案（B/C）會導致延遲
2. **性能可接受**: 使用適當索引後，查詢性能 <10ms，遠低於 100ms 目標
3. **實作簡單**: 無需維護複雜的快取失效邏輯，降低維護成本和 bug 風險
4. **可擴展性**: 未來若性能確實成為瓶頸，可在不改變 API 合約的情況下引入快取層

**實作細節**:
```sql
-- 優化後的查詢（使用複合索引）
CREATE INDEX idx_user_roles_userid_isdeleted ON user_roles(user_id, is_deleted);
CREATE INDEX idx_role_permissions_roleid ON role_permissions(role_id);
CREATE INDEX idx_permissions_code_isdeleted ON permissions(permission_code, is_deleted);
```

**備註**: 如果未來流量增長導致性能問題，可考慮引入短期快取（1-5 分鐘）搭配主動失效機制。

---

## 研究項目 2: 稽核日誌查詢效能（大量記錄）

### 問題描述
稽核日誌永久保留（符合 FR-024 合規要求），預期累積百萬級以上記錄。管理員需要能夠篩選查詢（按操作者、時間範圍、操作類型），且查詢回應時間需 <2000ms。

### 研究發現

#### 索引策略
**基礎索引**:
```sql
-- 操作時間索引（最常用的篩選條件，使用降序以支援「最新記錄」查詢）
CREATE INDEX idx_audit_logs_operation_time_desc ON audit_logs(operation_time DESC);

-- 操作者索引（篩選特定用戶操作）
CREATE INDEX idx_audit_logs_operator_id ON audit_logs(operator_id);

-- 操作類型索引（篩選特定操作類型）
CREATE INDEX idx_audit_logs_operation_type ON audit_logs(operation_type);

-- 複合索引：操作者 + 時間（最常見的組合查詢）
CREATE INDEX idx_audit_logs_operator_time ON audit_logs(operator_id, operation_time DESC);
```

**性能評估**:
- 單一條件查詢（時間範圍）: <500ms（100 萬記錄）
- 複合條件查詢（操作者 + 時間範圍）: <300ms（100 萬記錄）
- 分頁查詢（OFFSET/LIMIT）: <200ms（每頁 20-50 筆）

#### 分割表方案（Partitioning）
**實作方式**:
```sql
-- 按年份分割表（範圍分割）
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    operation_time TIMESTAMP NOT NULL,
    -- 其他欄位...
) PARTITION BY RANGE (operation_time);

-- 建立分割區
CREATE TABLE audit_logs_2025 PARTITION OF audit_logs
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

CREATE TABLE audit_logs_2026 PARTITION OF audit_logs
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
```

**優點**:
- 查詢時自動裁剪（Partition Pruning），只掃描相關分割區
- 維護更容易（可獨立備份/刪除舊分割區）
- 性能提升明顯（查詢速度提升 30-50%）

**缺點**:
- 初期設定複雜
- 需要定期建立新分割區（可自動化）
- 跨年度查詢可能稍慢（但仍符合需求）

#### JSON 欄位索引（GIN Index）
稽核日誌的「變更前狀態」和「變更後狀態」使用 JSON 格式儲存，可能需要查詢 JSON 內容：

```sql
-- GIN 索引支援 JSON 查詢
CREATE INDEX idx_audit_logs_before_state_gin ON audit_logs USING GIN (before_state);
CREATE INDEX idx_audit_logs_after_state_gin ON audit_logs USING GIN (after_state);
```

**評估**: 稽核日誌查詢主要是按時間、操作者、操作類型篩選，JSON 內容查詢為次要需求。初期不實作 GIN 索引，根據實際使用情況決定是否新增。

### 決策: 基礎索引策略（不使用分割表）

**理由**:
1. **初期簡化**: 基礎索引已能滿足 <2000ms 的性能需求
2. **避免過度設計**: 分割表增加初期實作複雜度，在記錄量達到 1000 萬級之前效益有限
3. **靈活調整**: 可在未來根據實際數據量和查詢模式決定是否引入分割表

**實作細節**:
```sql
-- 必要索引
CREATE INDEX idx_audit_logs_operation_time_desc ON audit_logs(operation_time DESC);
CREATE INDEX idx_audit_logs_operator_id ON audit_logs(operator_id);
CREATE INDEX idx_audit_logs_operation_type ON audit_logs(operation_type);
CREATE INDEX idx_audit_logs_operator_time ON audit_logs(operator_id, operation_time DESC);

-- 查詢優化：使用 LIMIT 和 OFFSET 進行分頁
SELECT * FROM audit_logs
WHERE operation_time BETWEEN @startTime AND @endTime
  AND operator_id = @operatorId
ORDER BY operation_time DESC
LIMIT @pageSize OFFSET @offset;
```

**監控計劃**: 設定資料庫查詢性能監控，當稽核日誌記錄數超過 500 萬且查詢時間接近 2000ms 時，考慮引入分割表。

---

## 研究項目 3: 權限繼承與合併邏輯複雜度

### 問題描述
當用戶被指派多個角色時，系統需要合併所有角色的權限（聯集）。需要明確權限衝突解決策略和有效權限計算邏輯。

### 研究發現

#### 權限合併策略

**策略 A: 聯集（Union）- 最寬鬆原則**
```text
用戶有效權限 = 角色A權限 ∪ 角色B權限 ∪ ... ∪ 角色N權限
```
- 只要用戶擁有的任一角色具有某權限，用戶就擁有該權限
- 符合 FR-015: 用戶擁有任一角色的權限即可執行相應操作

**策略 B: 交集（Intersection）- 最嚴格原則**
```text
用戶有效權限 = 角色A權限 ∩ 角色B權限 ∩ ... ∩ 角色N權限
```
- 用戶僅擁有所有角色共同擁有的權限
- 過於嚴格，實務上較少使用

**策略 C: 顯式拒絕（Explicit Deny）**
```text
用戶有效權限 = (角色允許權限 ∪) - (角色拒絕權限)
```
- 需要支援「拒絕權限」的概念
- 增加系統複雜度，規格中未要求

### 決策: 策略 A（聯集 - 最寬鬆原則）

**理由**:
1. **符合規格**: FR-015 明確要求「用戶擁有任一角色的權限即可執行相應操作」
2. **符合直覺**: 多數 RBAC 系統採用此策略（如 AWS IAM, Azure RBAC）
3. **實作簡單**: SQL 查詢使用 UNION 或 IN 即可實現
4. **無衝突問題**: 聯集操作不存在衝突（所有權限都是允許型）

**實作細節**:
```sql
-- 查詢用戶的所有有效權限（去重）
SELECT DISTINCT p.id, p.permission_code, p.permission_type, p.name, p.description
FROM permissions p
INNER JOIN role_permissions rp ON p.id = rp.permission_id
INNER JOIN user_roles ur ON rp.role_id = ur.role_id
WHERE ur.user_id = @userId 
  AND ur.is_deleted = false 
  AND p.is_deleted = false
ORDER BY p.permission_type, p.permission_code;
```

**邊界情況**:
- 用戶無任何角色: 返回空權限列表，所有權限驗證失敗
- 用戶角色被移除: 下次請求時重新計算有效權限
- 角色權限被修改: 下次請求時所有擁有該角色的用戶生效

---

## 研究項目 4: 併發寫入稽核日誌的性能影響

### 問題描述
每次權限管理操作（CRUD）都需要寫入稽核日誌。如果稽核日誌寫入失敗，FR-029 要求「必須回滾整個操作」。高併發場景下，同步寫入可能影響 API 響應時間。

### 研究發現

#### 方案 A: 同步寫入（在資料庫 Transaction 內）
**實作方式**:
```csharp
using var transaction = await _connection.BeginTransactionAsync();
try {
    // 1. 執行業務操作（如建立權限）
    await _permissionRepository.CreateAsync(permission, transaction);
    
    // 2. 寫入稽核日誌
    await _auditLogRepository.CreateAsync(auditLog, transaction);
    
    // 3. 提交 Transaction
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

**優點**:
- 保證資料一致性（符合 FR-029: 稽核日誌寫入失敗時回滾操作）
- 實作簡單，無需額外基礎設施
- 易於測試和除錯

**缺點**:
- 寫入稽核日誌增加 Transaction 時間（預估 +10-20ms）
- 高併發時資料庫寫入壓力較大

**性能評估**:
- 單次操作: 業務操作 50ms + 稽核日誌 15ms = 65ms（符合 <200ms 目標）
- 1000 TPS: 資料庫寫入 TPS 1000（PostgreSQL 可承受）

#### 方案 B: 非同步寫入（Message Queue）
**實作方式**:
```csharp
// 1. 執行業務操作
await _permissionRepository.CreateAsync(permission);

// 2. 發送稽核日誌到 Message Queue（非同步）
await _messageQueue.PublishAsync(auditLogMessage);

// 3. Background Worker 消費 Queue 並寫入資料庫
```

**優點**:
- API 響應時間不受稽核日誌寫入影響
- 高吞吐量

**缺點**:
- **違反 FR-029**: 稽核日誌寫入失敗時無法回滾業務操作
- 增加系統複雜度（需要 RabbitMQ 或 Redis）
- 稽核日誌與業務操作不在同一 Transaction，存在不一致風險
- Message Queue 失敗時日誌遺失

#### 方案 C: 批次寫入（Bulk Insert）
**實作方式**:
```csharp
// 收集多個稽核日誌，定期批次寫入
var logs = new List<AuditLog>();
logs.Add(auditLog);

if (logs.Count >= 100 || _timer.Elapsed > TimeSpan.FromSeconds(5)) {
    await _auditLogRepository.BulkInsertAsync(logs);
    logs.Clear();
}
```

**優點**:
- 減少資料庫寫入次數，提升吞吐量

**缺點**:
- **違反 FR-029**: 無法保證稽核日誌與業務操作的原子性
- 批次寫入失敗時影響多個操作的日誌
- 實作複雜度高

### 決策: 方案 A（同步寫入，在 Transaction 內）

**理由**:
1. **符合規格**: FR-029 明確要求「稽核日誌寫入失敗時必須回滾操作」，只有方案 A 能保證
2. **性能可接受**: 寫入稽核日誌增加 10-20ms，總響應時間仍遠低於 200ms 目標
3. **資料一致性**: Transaction 保證業務操作與稽核日誌的原子性
4. **實作簡單**: 無需引入額外基礎設施（Message Queue），降低維護成本

**實作細節**:
```csharp
// Service Layer
public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, Guid operatorId) {
    using var transaction = await _connection.BeginTransactionAsync();
    try {
        // 1. 建立權限
        var permission = await _permissionRepository.CreateAsync(new Permission { ... }, transaction);
        
        // 2. 記錄稽核日誌
        var auditLog = new AuditLog {
            OperatorId = operatorId,
            OperationType = "新增權限",
            BeforeState = null,
            AfterState = JsonSerializer.Serialize(permission),
            // ... 其他欄位
        };
        await _auditLogRepository.CreateAsync(auditLog, transaction);
        
        // 3. 提交
        await transaction.CommitAsync();
        return MapToDto(permission);
    } catch {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**優化建議**:
- 稽核日誌表使用獨立的 tablespace（如果資料庫支援）以分散 I/O
- 稽核日誌表的索引最少化（僅保留必要的查詢索引）以加速寫入
- 監控資料庫 Transaction 時間，如未來成為瓶頸再考慮其他方案

---

## 其他技術決策

### 權限代碼格式規範
**決策**: 採用 `resource.action` 格式（如 `inventory.create`, `users.delete`）

**理由**:
- 符合 FR-005 規範
- 易於理解和管理
- 支援批次權限驗證（前端可一次查詢多個資源的權限）

**驗證規則**:
```csharp
// FluentValidation
RuleFor(x => x.PermissionCode)
    .Matches(@"^[a-z]+\.[a-z]+$")
    .WithMessage("權限代碼格式錯誤，必須為 resource.action 格式（如 inventory.create）");
```

### 路由權限格式
**決策**: 使用前端路由路徑（如 `/inventory`, `/users/profile`）

**理由**:
- 符合 FR-004 規範
- 與前端路由直接對應，易於整合
- 支援路由層級驗證（如 `/inventory/*`）

### 軟刪除實作
**決策**: 所有實體（Permission, Role）使用軟刪除（is_deleted, deleted_at, deleted_by）

**理由**:
- 符合 constitution 中 User 實體的現有模式
- 保留歷史記錄供審計
- 關聯檢查時僅考慮未刪除的記錄

### 樂觀並發控制
**決策**: 僅在 Permission 和 Role 實體使用 version 欄位（與 User 實體一致）

**理由**:
- 防止併發修改衝突
- 符合 constitution 要求和現有模式
- UserRole 和 RolePermission 關聯表不需要（主要是新增/刪除操作）

---

## 總結

本研究報告解決了所有 NEEDS CLARIFICATION，技術決策總結如下：

| 研究項目 | 決策 | 理由 |
|---------|------|------|
| 權限驗證性能優化 | 即時查詢（無快取） | 符合「即時生效」需求，性能可接受，實作簡單 |
| 稽核日誌查詢效能 | 基礎索引策略（不使用分割表） | 初期簡化，性能已滿足需求，未來可擴展 |
| 權限合併邏輯 | 聯集（最寬鬆原則） | 符合規格 FR-015，實作簡單，無衝突問題 |
| 併發寫入稽核日誌 | 同步寫入（Transaction 內） | 符合規格 FR-029（失敗回滾），性能可接受 |

所有決策均已考慮規格需求、性能目標、實作複雜度和可維護性，並提供了未來擴展的可能性。
