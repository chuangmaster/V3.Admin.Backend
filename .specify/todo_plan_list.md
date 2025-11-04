# 待辦清單與規劃事項

**更新日期**: 2025-11-05  
**目的**: 記錄跨週期需要注意的事項、過去功能分支的技術債務、以及未來開發週期的改進項目

---

## 憲章更新影響 (v1.4.0 - 2025-11-05)

### 資料庫命名規範標準化

**憲章變更**: 
- 版本升級至 v1.4.0
- Principle I 新增 Database Naming Standards (snake_case for DB, PascalCase for C#)
- 明確定義 tables/columns/indexes/constraints 命名模式

**影響範圍**:
- ✅ 憲章主檔已更新 (`.specify/memory/constitution.md`)
- ✅ 模板已更新 (`.specify/templates/plan-template.md`)
- ⚠️ 過去功能分支規格需在**下次相關開發時**審查更新

**待處理項目**:

### 1. 001-account-management (已合併分支)

**狀態**: 已完成並合併至 main  
**優先級**: P3 (文件改進,非阻塞)

**需要更新的檔案**:
- `specs/001-account-management/plan.md`
  - Constitution Check 缺少資料庫命名規範檢查項
  - Phase 0/Phase 1 檢查可補充「資料庫 snake_case 命名」說明
  
- `specs/001-account-management/data-model.md`
  - 命名規則章節可擴充資料庫命名標準 (snake_case)
  - 可新增映射關係說明 (PascalCase ↔ snake_case)
  - 可補充索引/約束命名範例

**建議處理時機**:
- 當需要參考 001 規格進行類似功能開發時
- 或當 001 功能需要擴充/重構時
- 或進行文件全面審查時

**注意事項**:
- 實作程式碼已符合 snake_case 規範 (`users` 表, `User.cs` 實體映射)
- 無需修改程式碼,僅需補充文件說明
- 不影響當前系統運作

---

## 未來開發週期注意事項

### 新功能開發檢查清單

所有新功能分支 (例如: `002-permission-management`, `003-xxx`) **必須**遵循:

1. **資料庫設計**:
   - ✅ 資料表命名: snake_case 複數形式 (e.g., `user_roles`, `permissions`)
   - ✅ 欄位命名: snake_case (e.g., `role_id`, `created_at`, `is_active`)
   - ✅ 索引命名: `idx_tablename_columnname` 模式
   - ✅ 約束命名: `chk_`/`fk_` 前綴 + 表名 + 描述

2. **C# 實體設計**:
   - ✅ 類別/屬性: PascalCase (e.g., `UserRole.RoleId`, `Permission.CreatedAt`)
   - ✅ Dapper/EF 映射: 明確指定 column name 映射至 snake_case

3. **文件要求**:
   - ✅ `plan.md` Constitution Check 必須包含資料庫命名規範驗證
   - ✅ `data-model.md` 必須說明 PascalCase ↔ snake_case 映射關係
   - ✅ 遷移腳本註解必須說明命名邏輯

---

## 技術債務追蹤

*目前無技術債務項目*

---

## 模板改進建議

*目前無待改進項目 - 所有模板已更新至 v1.4.0 憲章標準*

---

## 更新記錄

| 日期 | 類型 | 說明 | 負責人 |
|------|------|------|--------|
| 2025-11-05 | 憲章更新 | v1.4.0 新增資料庫命名規範 | - |
| 2025-11-05 | 待辦新增 | 記錄 001-account-management 文件改進項目 | - |

