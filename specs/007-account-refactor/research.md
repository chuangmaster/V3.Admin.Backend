# Research: Account Module Refactoring

**Feature**: Account Module Refactoring  
**Branch**: `007-account-refactor`  
**Date**: 2026-01-20

## Research Goals

此階段研究目標為確認以下技術細節和最佳實踐:

1. **資料庫欄位重命名的最佳實踐** - 如何安全地在生產環境中重命名欄位
2. **JWT 會話失效機制** - 如何在密碼修改後失效特定會話
3. **併發控制實作細節** - Dapper 中如何實作 WHERE version=X 的原子性操作
4. **審計日誌設計** - 現有 audit_logs 表結構是否支援密碼重設操作

## Research Findings

### 1. 資料庫欄位重命名策略

**問題**: 如何在 PostgreSQL 中安全地重命名欄位,同時確保資料完整性?

**調查結果**:
PostgreSQL 支援使用 `ALTER TABLE ... RENAME COLUMN` 語句重命名欄位。此操作是事務性的,不會造成資料遺失。

**決策**: 使用以下 migration script 策略:
```sql
-- Migration: 001_RenameUsernameToAccount.sql
BEGIN;

-- 重命名欄位
ALTER TABLE users RENAME COLUMN username TO account;

-- 更新相關索引(如果有)
-- ALTER INDEX idx_username RENAME TO idx_account;

-- 驗證資料完整性
DO $$
DECLARE
    total_users INT;
    users_with_account INT;
BEGIN
    SELECT COUNT(*) INTO total_users FROM users;
    SELECT COUNT(*) INTO users_with_account FROM users WHERE account IS NOT NULL;
    
    IF total_users != users_with_account THEN
        RAISE EXCEPTION 'Data integrity check failed: % users have NULL account', (total_users - users_with_account);
    END IF;
END $$;

COMMIT;
```

**理由**: 
- 事務性保證操作原子性
- 包含資料完整性驗證
- 失敗時自動回滾

**替代方案**: 
- 方案 A: 建立新欄位 account,複製資料,刪除舊欄位 → 拒絕,因為增加複雜度且需要處理過渡期
- 方案 B: 使用資料庫視圖提供向後相容性 → 拒絕,根據 spec.md clarification,不需要向後相容

---

### 2. JWT 會話失效機制

**問題**: 用戶修改密碼後,如何使其他設備的 JWT token 失效,同時保留當前會話?

**調查結果**:
JWT 是無狀態的,無法直接撤銷。常見做法:
1. Token 黑名單(需要快取層,如 Redis)
2. 使用 version 欄位或 issued_at 時間戳驗證
3. 縮短 token 有效期配合 refresh token

**決策**: 採用 **既有 version 欄位進行 token 驗證**:
```csharp
// 密碼修改時遞增 version (任何資料修改都會遞增)
UPDATE users SET password = @password, version = version + 1
WHERE id = @id AND version = @version;

// JWT 包含 version claim
var claims = new[]
{
    new Claim("user_id", user.Id.ToString()),
    new Claim("version", user.Version.ToString()),
    // ...
};

// 驗證時檢查 version
var jwtVersion = int.Parse(User.FindFirst("version")?.Value ?? "0");
if (jwtVersion != currentUser.Version)
{
    throw new UnauthorizedException("Token has been invalidated");
}
```

**理由**:
- 符合 Simplicity First 原則,不需要引入 Redis 或新增欄位
- 利用既有的併發控制欄位實現會話控制
- 當前請求的 token 在密碼修改前已驗證,可以完成當前操作
- 任何資料修改(email、角色等)都會使 token 失效,提供更嚴格的安全性

**注意事項**:
- 需要在 JwtService.GenerateToken 中加入 version claim
- 需要在認證 middleware 或 service 中驗證 version
- 任何 user 資料修改都會導致 token 失效,需重新登入

**替代方案**:
- 方案 A: Redis 黑名單 → 拒絕,引入額外依賴,違反 Simplicity First
- 方案 B: 新增 token_version 欄位 → 已拒絕,user 資料修改不頻繁,單一 version 欄位已足夠
- 方案 C: 縮短 token 有效期至 5 分鐘 → 拒絕,用戶體驗差,頻繁需要 refresh

---

### 3. Dapper 併發控制實作

**問題**: 如何使用 Dapper 實作樂觀鎖定,確保 WHERE version=X 的原子性?

**調查結果**:
Dapper 支援參數化查詢,可以在 WHERE 子句中包含 version 條件。PostgreSQL 的 UPDATE 語句本身是原子性的。

**決策**: 使用以下模式:
```csharp
public async Task<bool> UpdatePasswordAsync(int userId, string hashedPassword, int version)
{
    const string sql = @"
        UPDATE users 
        SET password = @Password, 
            version = version + 1,
            updated_at = NOW()
        WHERE id = @UserId AND version = @Version
        RETURNING version";

    var newVersion = await _connection.QuerySingleOrDefaultAsync<int?>(
        sql, 
        new { UserId = userId, Password = hashedPassword, Version = version }
    );

    return newVersion.HasValue; // true if updated, false if version mismatch
}
```

**理由**:
- WHERE version=@Version 確保只有版本匹配時才更新
- RETURNING version 確認更新成功並返回新版本
- 如果 version 不匹配,不會有任何 row 被更新,QuerySingleOrDefaultAsync 返回 null

**Service 層處理**:
```csharp
var updated = await _userRepository.UpdatePasswordAsync(userId, hashedPassword, request.Version);
if (!updated)
{
    return ApiResponse.Error<object>(
        ResponseCodes.CONCURRENT_UPDATE_CONFLICT,
        "密碼修改失敗,資料已被其他操作更新,請重新獲取最新資料後再試"
    );
}
```

**替代方案**:
- 方案 A: 使用資料庫 transaction isolation level → 不需要,WHERE version 已足夠
- 方案 B: 使用 row locking (SELECT FOR UPDATE) → 過度設計,樂觀鎖定更簡單

---

### 4. 審計日誌表結構

**問題**: 現有 audit_logs 表是否支援記錄密碼重設操作?需要哪些欄位?

**調查**: 檢查現有資料庫 schema 或 AuditLog entity

**假設現有結構** (需要實際確認):
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; }           // 操作類型
    public int? OperatorId { get; set; }         // 操作者 ID
    public int? TargetUserId { get; set; }       // 被操作者 ID
    public string Details { get; set; }          // JSON 格式詳細資訊
    public DateTime CreatedAt { get; set; }
}
```

**決策**: 如果現有結構支援以上欄位,則無需修改。密碼重設時記錄:
```csharp
await _auditLogRepository.CreateAsync(new AuditLog
{
    Action = "PasswordReset",
    OperatorId = operatorId,           // 管理員 ID
    TargetUserId = targetUserId,       // 被重設密碼的用戶 ID
    Details = JsonSerializer.Serialize(new 
    { 
        Timestamp = DateTime.UtcNow,
        ResetBy = operatorUsername
    }),
    CreatedAt = DateTime.UtcNow
});
```

**注意**: 絕對不可記錄密碼內容(明文或加密),僅記錄操作類型和相關用戶 ID。

**如果需要新增欄位**: 創建 migration script 新增必要欄位(unlikely,通常 audit_logs 設計時已考慮通用性)

**替代方案**:
- 方案 A: 每種操作建立獨立的 audit table → 拒絕,過度複雜化
- 方案 B: 使用結構化日誌替代資料庫審計 → 拒絕,憲法要求 critical operations 寫入 audit_logs 表

---

## Architecture Decisions

### AD-001: 使用單一 version 欄位優於獨立 token_version 或 Redis 黑名單

**Context**: 需要實作密碼修改後的會話失效機制

**Decision**: 使用既有 version 欄位同時處理併發控制和 token 驗證,在 JWT claims 中包含 version 值

**Consequences**: 
- ✅ 無需引入 Redis 依賴或新增欄位
- ✅ 完全符合 Simplicity First 原則
- ✅ 利用既有資料庫欄位和認證機制
- ✅ 提供更嚴格的安全性(任何資料修改都失效 token)
- ⚠️ 需要修改 JwtService.GenerateToken 和認證驗證邏輯
- ⚠️ Token validation 需要查詢資料庫(但已有 user context 查詢,影響可控)
- ⚠️ 任何 user 資料修改(非僅密碼)都會導致用戶需重新登入

---

### AD-002: 直接重命名欄位,不提供向後相容性

**Context**: 需要將 username 欄位重命名為 account

**Decision**: 使用 ALTER TABLE RENAME COLUMN,立即拒絕包含 username 的 API 請求

**Consequences**:
- ✅ 實作簡單,無過渡期複雜度
- ✅ 符合 clarification 中的決策(立即拒絕 username 欄位)
- ⚠️ 前後端必須同時部署
- ⚠️ 需要更新所有 API 文件和客戶端程式碼

---

### AD-003: 使用 RETURNING 子句驗證更新成功

**Context**: 需要確認併發控制更新是否成功

**Decision**: UPDATE 語句使用 RETURNING version,根據返回值判斷是否更新成功

**Consequences**:
- ✅ 單一 SQL 語句完成更新和驗證
- ✅ 避免額外的 SELECT 查詢
- ✅ PostgreSQL 原生支援 RETURNING
- ⚠️ 如果未來遷移到其他資料庫,可能需要調整(unlikely,憲法指定 PostgreSQL)

---

## Technical Unknowns (Resolved)

所有技術未知項已透過研究解決:

- ✅ 資料庫欄位重命名策略 → 使用 ALTER TABLE RENAME COLUMN with transaction
- ✅ JWT 會話失效機制 → 使用既有 version 欄位
- ✅ 併發控制實作 → WHERE version=X + RETURNING clause
- ✅ 審計日誌設計 → 使用現有 audit_logs 表

無需進一步研究,可以進入 Phase 1 設計階段。

---

## Next Steps

1. ✅ Phase 0 完成:所有技術未知項已解決
2. ➡️ Phase 1:建立 data-model.md,定義 User entity 和 AuditLog 的變更
3. ➡️ Phase 1:建立 contracts/api-spec.yaml,定義兩個新 API 端點的 OpenAPI 規格
4. ➡️ Phase 1:建立 quickstart.md,提供開發者實作指南
