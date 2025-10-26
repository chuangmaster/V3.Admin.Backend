# Implementation Plan: 帳號管理系統

**Branch**: `001-account-management` | **Date**: 2025-10-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-account-management/spec.md`
**Language**: This plan MUST be written in Traditional Chinese (zh-TW) per constitution requirements

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

本功能提供完整的帳號管理系統,包含帳號密碼登入、新增帳號、修改個人資料(密碼與姓名)、刪除帳號等功能。系統採用 .NET 9 與 ASP.NET Core Web API 開發,使用 PostgreSQL 作為資料庫並透過 Dapper 進行資料存取。架構遵循三層式設計(Controller/Service/Repository)並使用 DTO 模式進行資料傳輸。所有 API 回應採用 ApiResponseModel 包裝,實現雙層回應設計(HTTP 狀態碼 + 業務邏輯代碼),並包含 TraceId 支援分散式追蹤。

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: 
- ASP.NET Core 9 Web API
- Npgsql (PostgreSQL driver)
- Dapper (micro-ORM)
- Microsoft.AspNetCore.Authentication.JwtBearer
- BCrypt.Net-Next (密碼雜湊)
- FluentValidation (輸入驗證)
**Storage**: PostgreSQL 15+ (透過 Dapper 進行資料存取)
**Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing (整合測試)
**Target Platform**: 跨平台 (Windows/Linux/macOS)
**Project Type**: Web API - ASP.NET Core 後端服務
**Performance Goals**: 
- 簡單操作 <200ms (登入、CRUD 單一帳號)
- 複雜操作 <2000ms (批次查詢、關聯操作)
**Constraints**: 
- JWT 身份驗證 (Bearer Token)
- 所有 API 回應使用 ApiResponseModel (雙層設計: HTTP 狀態碼 + 業務邏輯代碼)
- 錯誤訊息使用繁體中文
- TraceId 支援分散式追蹤
- 密碼使用 BCrypt 雜湊儲存
- 帳號採用軟刪除機制
- 輸入驗證使用 FluentValidation
**Scale/Scope**: 
- 基礎帳號管理系統 (登入、新增、修改、刪除)
- 支援 100+ 並發請求
- 單體應用 (monolithic API)
- 未來可擴展至角色權限管理

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Phase 0 檢查 (研究階段)**: ✅ 已通過
- Code Quality Excellence: ✅ C# 13 最佳實踐、XML 文件註解、繁體中文註解
- Three-Layer Architecture: ✅ Controller/Service/Repository 三層架構
- Test-First Development: ✅ 測試先行開發,xUnit + Moq
- User Experience Consistency: ✅ ApiResponseModel 雙層設計、TraceId 追蹤
- Performance & Security: ✅ BCrypt 密碼雜湊、JWT 驗證、FluentValidation

**Phase 1 檢查 (設計階段)**: ✅ 已通過
- Data Model: ✅ User 實體、5 個 Request DTOs、3 個 Response DTOs、完整驗證器
- API Contracts: ✅ OpenAPI 3.0 規格、6 個端點、標準化錯誤碼
- Architecture: ✅ 三層架構明確定義、職責清晰分離、DTO 模式
- Security: ✅ JWT Bearer Token、BCrypt (work factor 12)、軟刪除、樂觀並發控制
- Performance: ✅ 索引策略、連接池、查詢優化、<200ms 簡單操作
- Documentation: ✅ research.md、data-model.md、api-spec.yaml、quickstart.md、繁體中文

**憲法合規性**: ✅ 無違規事項,所有設計決策完全符合專案憲法要求。

**Phase 1 產出清單**:
- ✅ research.md (13 個技術決策,含最佳實踐)
- ✅ data-model.md (實體、DTOs、驗證器、業務規則、遷移腳本)
- ✅ contracts/api-spec.yaml (OpenAPI 3.0 完整規格)
- ✅ quickstart.md (開發設定、API 範例、測試、FAQ)
- ✅ .github/copilot-instructions.md (代理上下文已更新)

## Project Structure

### Documentation (this feature)

```text
specs/001-account-management/
├── plan.md              # 本檔案 (/speckit.plan 指令輸出)
├── research.md          # Phase 0 輸出 (技術研究與決策)
├── data-model.md        # Phase 1 輸出 (資料模型設計)
├── quickstart.md        # Phase 1 輸出 (快速入門指南)
├── contracts/           # Phase 1 輸出 (API 合約規格)
│   └── api-spec.yaml   # OpenAPI 3.0 規格
└── tasks.md             # Phase 2 輸出 (/speckit.tasks 指令 - 非 /speckit.plan 建立)
```

### Source Code (repository root)

```text
Controllers/                      # 展示層 - API 端點
├── AuthController.cs            # 登入/登出控制器 (已存在)
├── AccountController.cs         # 帳號管理控制器 (新增)
└── BaseApiController.cs         # 基礎控制器 (已存在)

Services/                         # 業務邏輯層
├── AuthService.cs               # 身份驗證服務 (已存在)
├── AccountService.cs            # 帳號管理服務 (新增)
└── Interfaces/
    ├── IAuthService.cs          # (已存在)
    └── IAccountService.cs       # (新增)

Repositories/                     # 資料存取層
├── UserRepository.cs            # 使用者資料存取 (已存在,需擴充)
└── Interfaces/
    └── IUserRepository.cs       # (已存在,需擴充)

Models/                           # 資料模型與 DTO
├── User.cs                      # 使用者實體 (已存在,需擴充)
├── LoginRequest.cs              # 登入請求 DTO (已存在)
├── ApiResponseModel.cs          # API 回應模型 (已存在)
├── CreateAccountRequest.cs      # 新增帳號請求 DTO (新增)
├── UpdateAccountRequest.cs      # 更新帳號請求 DTO (新增)
├── ChangePasswordRequest.cs     # 變更密碼請求 DTO (新增)
├── AccountResponse.cs           # 帳號回應 DTO (新增)
└── ResponseCodes.cs             # 回應代碼常數 (新增)

Configuration/                    # 組態設定 (新增)
├── JwtSettings.cs               # JWT 組態模型
└── DatabaseSettings.cs          # 資料庫組態模型

Validators/                       # 輸入驗證器 (新增)
├── CreateAccountRequestValidator.cs
├── UpdateAccountRequestValidator.cs
└── ChangePasswordRequestValidator.cs

Middleware/                       # 中介軟體 (新增)
├── ExceptionHandlingMiddleware.cs  # 全域異常處理
└── TraceIdMiddleware.cs            # TraceId 注入

Tests/                            # 測試專案
├── Unit/                        # 單元測試
│   ├── Services/
│   └── Validators/
└── Integration/                 # 整合測試
    └── Controllers/

Database/                         # 資料庫相關 (新增)
├── Migrations/                  # 資料庫遷移腳本
│   └── 001_CreateUsersTable.sql
└── Scripts/                     # 輔助腳本
    └── seed.sql                # 初始資料
```

**結構決策**: 採用 ASP.NET Core 三層架構,Controller (展示層) 負責 HTTP 請求處理,Service (業務邏輯層) 負責商業邏輯,Repository (資料存取層) 負責資料庫操作。所有介面放置於各自的 Interfaces 資料夾以維持清晰的關注點分離。使用 Dapper 作為 micro-ORM 進行 PostgreSQL 資料存取。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**無複雜度違規**: 本設計完全符合專案憲法要求,無需特殊理由說明。

---

## Phase 0 & Phase 1 完成總結

### 執行狀態

✅ **Phase 0: Outline & Research** - 已完成  
✅ **Phase 1: Design & Contracts** - 已完成  
⏸️ **Phase 2: Task Breakdown** - 待執行 (使用 `/speckit.tasks` 指令)

### 產出文件

| 文件 | 狀態 | 位置 | 描述 |
|-----|------|------|------|
| plan.md | ✅ | specs/001-account-management/ | 實作計劃 (本檔案) |
| research.md | ✅ | specs/001-account-management/ | 技術研究與決策 (13 個技術選擇) |
| data-model.md | ✅ | specs/001-account-management/ | 資料模型設計 (實體、DTOs、驗證器) |
| api-spec.yaml | ✅ | specs/001-account-management/contracts/ | OpenAPI 3.0 API 規格 (6 個端點) |
| quickstart.md | ✅ | specs/001-account-management/ | 快速入門指南 (環境設定、API 範例) |
| copilot-instructions.md | ✅ | .github/ | 代理上下文 (已更新技術堆疊) |

### 關鍵設計決策摘要

1. **技術堆疊**: .NET 9 + ASP.NET Core + PostgreSQL + Dapper
2. **架構**: 三層式架構 (Controller/Service/Repository) + DTO 模式
3. **身份驗證**: JWT Bearer Token (1 小時有效期)
4. **密碼安全**: BCrypt (work factor 12)
5. **資料驗證**: FluentValidation
6. **API 回應**: ApiResponseModel 雙層設計 (HTTP 狀態碼 + 業務邏輯代碼)
7. **追蹤機制**: TraceId 支援分散式追蹤
8. **刪除策略**: 軟刪除 (保留資料與審計追蹤)
9. **並發控制**: 樂觀並發控制 (版本號)
10. **測試策略**: xUnit + Moq + Testcontainers

### 憲法合規檢查

✅ **Code Quality Excellence**: C# 13 最佳實踐、XML 文件註解、繁體中文註解  
✅ **Three-Layer Architecture**: 明確的職責分離與介面定義  
✅ **Test-First Development**: 完整的測試策略 (單元測試 + 整合測試)  
✅ **User Experience Consistency**: 統一的 API 回應格式與錯誤處理  
✅ **Performance & Security**: <200ms 回應時間、JWT 驗證、BCrypt 雜湊  
✅ **API Response Design**: 雙層回應設計、TraceId、標準化錯誤碼

### 下一步驟

**立即行動**:
```powershell
# 使用 /speckit.tasks 指令建立詳細的實作任務清單
# 這將生成 tasks.md 檔案,包含可執行的開發任務
```

**開發流程**:
1. 執行 `/speckit.tasks` 生成任務清單
2. 按照 tasks.md 中的任務順序進行開發
3. 遵循測試先行開發 (TDD) 原則
4. 定期檢視憲法合規性
5. 完成後執行完整測試套件

**參考文件**:
- 技術細節與最佳實踐 → [research.md](./research.md)
- 資料模型與驗證規則 → [data-model.md](./data-model.md)
- API 合約與端點規格 → [contracts/api-spec.yaml](./contracts/api-spec.yaml)
- 開發環境設定與範例 → [quickstart.md](./quickstart.md)

---

**Branch**: `001-account-management`  
**Plan Created**: 2025-10-26  
**Plan Status**: Phase 0 & Phase 1 Complete ✅  
**Ready for**: Phase 2 Task Breakdown (use `/speckit.tasks` command)
