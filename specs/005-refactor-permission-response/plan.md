# 實施計畫：重構 PermissionController API 回應

**分支**: `005-refactor-permission-response` | **日期**: 2025-11-25 | **規格**: [spec.md](./spec.md)
**來源**: 功能規格書位於 `/specs/005-refactor-permission-response/spec.md`

**注意**: 此範本由 `/speckit.plan` 命令生成。執行流程請參閱 `.specify/templates/commands/plan.md`。

## 總結

本計畫旨在重構 `PermissionController` 及其相關 API，使其完全符合專案 constitution 中定義的 `Principle VIII: Controller Response DTO Architecture`。核心任務是將目前直接回傳服務層 DTO 的端點，改為在 Controller 層手動映射到專用的 `xxxResponse` DTO，以達成 API 合約與內部業務邏輯的徹底解耦。根據最新需求，`PermissionResponse` DTO 將包含 `PermissionType` 欄位。

## 技術背景

**語言/版本**: C# (Target Framework: net9.0)
**主要依賴**: ASP.NET Core, Dapper, FluentValidation, Npgsql
**儲存**: PostgreSQL
**測試**: xUnit (根據 `V3.Admin.Backend.Tests.csproj` 推斷，需確認)
**目標平台**: Web API (Linux/Windows Server)
**專案類型**: Web Application (Backend)
**效能目標**: 遵循 constitution VII，簡單操作 < 200ms，複雜查詢 < 2000ms。
**限制**: 必須維持現有 API 的公開 JSON 結構不變，或記錄任何必要的變更。
**規模/範圍**: 影響 `PermissionController` 的所有端點，以及相關的 Service DTO 與新建立的 Response DTO。

## Constitution 檢查

*閘門：必須在階段 0 研究前通過。在階段 1 設計後重新檢查。*

- **I. Code Quality Excellence**: ✅ 通過。新程式碼將遵循 C# 13 最佳實踐，並添加必要的中文註釋。
- **II. Three-Layer Architecture**: ✅ 通過。此重構將強化三層架構中 Presentation 層的職責，使其更清晰。
- **VIII. Controller Response DTO Architecture**: ✅ 通過。本計畫的核心目標就是實現此原則。
- **IX. Paginated Response Design**: ✅ 通過。如果 `PermissionController` 中存在分頁端點，將確保其使用 `PagedApiResponseModel`。
- **API Response Design Standards**: ✅ 通過。將確保所有回應都透過 `BaseController` 的輔助方法（如 `Success()`）產生。

**結論**: 本計畫完全符合 constitution，旨在修正現有程式碼以達到合規標準。

## 專案結構

### 文件 (本功能)

```text
specs/005-refactor-permission-response/
├── plan.md              # 本檔案
├── research.md          # 階段 0 產出
├── data-model.md        # 階段 1 產出
├── quickstart.md        # 階段 1 產出
├── contracts/           # 階段 1 產出
│   └── README.md
└── tasks.md             # 階段 2 產出 (非本命令生成)
```

### 原始碼 (儲存庫根目錄)

```text
# Web application (backend)
V3.Admin.Backend/
├── Controllers/
│   └── PermissionController.cs      # 主要修改目標
├── Models/
│   ├── Dtos/
│   │   └── PermissionDto.cs         # 來源 DTO
│   └── Responses/
│       ├── PermissionResponse.cs    # 新增/修改
│       └── PermissionListResponse.cs # 新增/修改
├── Services/
│   └── PermissionService.cs         # 維持不變
└── Repositories/
    └── PermissionRepository.cs      # 維持不變
```

**結構決策**: 採用現有的 Web Application 後端結構。主要工作集中在 `Controllers` 和 `Models/Responses` 目錄，以實現架構分離，不動到業務或資料存取層。

## 複雜度追蹤

> **僅在 Constitution 檢查有必須證明的違規時填寫**

| 違規 | 為何需要 | 被拒絕的更簡單替代方案 |
|-----------|------------|-------------------------------------|
| (無)      | (無)       | (無)                                |