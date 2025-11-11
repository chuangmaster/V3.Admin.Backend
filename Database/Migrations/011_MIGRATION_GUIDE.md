## 資料庫遷移文件 - Migration 011

**Feature**: 003-user-profile (用戶個人資料查詢 API)  
**Migration Number**: 011  
**Date**: 2025-11-12  
**Status**: ✅ Ready for Deployment  

---

### 概述

此遷移為「用戶個人資料查詢 API」功能新增必要的資料庫權限定義。

**目標**: 在 `permissions` 表中新增 `user.profile.read` 權限，允許用戶查詢自己的個人資料（包含用戶名稱、顯示名稱和角色清單）。

---

### 遷移內容

#### 新增的權限

| 欄位 | 值 |
|------|-----|
| **permission_code** | `user.profile.read` |
| **name** | 查詢個人資料 |
| **description** | 允許用戶查詢自己的個人資料，包括用戶名稱、顯示名稱和角色 |
| **permission_type** | `function` |
| **route_path** | NULL (功能權限，不需要路由) |

#### API 端點對應

- **端點**: `GET /api/account/me`
- **所需權限**: `user.profile.read`
- **HTTP 狀態碼**:
  - `200 OK`: 查詢成功
  - `401 Unauthorized`: 未授權
  - `403 Forbidden`: 無權限
  - `404 Not Found`: 用戶不存在

---

### 執行說明

#### 方式 1: 使用遷移腳本 (推薦)

```bash
# 執行遷移
psql -h <host> -U <user> -d <database> -f Database/Migrations/011_AddUserProfileReadPermission.sql

# 或在 pgAdmin 中執行腳本內容
```

#### 方式 2: 使用 seed 腳本 (初始化時)

如果是新環境初始化，在執行主遷移後運行 seed 腳本：

```bash
psql -h <host> -U <user> -d <database> -f Database/Scripts/seed_permissions.sql
```

---

### 驗證步驟

執行遷移後，驗證權限已成功建立：

```sql
-- 驗證 user.profile.read 權限存在
SELECT id, permission_code, name, permission_type, is_deleted
FROM permissions
WHERE permission_code = 'user.profile.read';
```

預期結果：

| id | permission_code | name | permission_type | is_deleted |
|-----|-----------------|------|-----------------|-----------|
| (uuid) | user.profile.read | 查詢個人資料 | function | false |

---

### 相依性

- ✅ 無須其他遷移
- ✅ 不需要修改表結構
- ✅ 不需要資料轉換
- ✅ 向後相容 (只新增，不修改)

---

### 回滾指示

若需要回滾此遷移，執行以下操作：

```sql
-- 軟刪除 user.profile.read 權限
UPDATE permissions
SET is_deleted = true,
    deleted_at = CURRENT_TIMESTAMP,
    deleted_by = (SELECT id FROM users WHERE username = 'admin' LIMIT 1)
WHERE permission_code = 'user.profile.read' AND is_deleted = false;
```

或完全刪除（如果需要）：

```sql
-- 完全刪除權限記錄 (不建議使用)
DELETE FROM permissions
WHERE permission_code = 'user.profile.read';
```

---

### 相關檔案

- **遷移檔**: `Database/Migrations/011_AddUserProfileReadPermission.sql`
- **Seed 腳本**: `Database/Scripts/seed_permissions.sql`
- **API 規格**: `specs/V3.Admin.Backend.API.yaml`
- **功能規格**: `specs/003-user-profile/spec.md`

---

### 變更日誌

| 日期 | 版本 | 說明 |
|------|------|------|
| 2025-11-12 | 1.0 | 初版 - 新增 user.profile.read 權限 |

---

### 注意事項

1. **自動執行**: 此遷移使用 `ON CONFLICT (permission_code) DO NOTHING`，即使權限已存在也不會報錯
2. **安全性**: 權限代碼遵循命名慣例 `resource.action` 格式
3. **稽核追蹤**: 權限記錄包含 `created_at`, `updated_at`, `version` 等追蹤欄位
4. **驗證通知**: 遷移完成後會輸出驗證訊息

---

### 部署檢清單

在執行此遷移前，請確認：

- [ ] 備份資料庫
- [ ] 在測試環境執行並驗證
- [ ] 檢查 permissions 表是否存在
- [ ] 確認沒有其他進行中的遷移
- [ ] 記錄遷移執行時間和任何警告訊息
- [ ] 執行後驗證權限已成功建立

執行後，請確認：

- [ ] 新權限在資料庫中可見
- [ ] GET /api/account/me 端點已正確使用此權限
- [ ] 沒有資料不一致的問題
- [ ] 應用程式日誌無異常

---

### 支援

如有問題，請參考：

- 功能規格: `specs/003-user-profile/spec.md`
- API 文件: `specs/V3.Admin.Backend.API.yaml`
- 實作計劃: `specs/003-user-profile/plan.md`
