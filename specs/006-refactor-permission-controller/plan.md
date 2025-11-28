# Implementation Plan: 權限控制器回傳模型重構

**Branch**: `006-refactor-permission-controller` | **Date**: 2025年11月25日 | **Spec**: [specs/006-refactor-permission-controller/spec.md](specs/006-refactor-permission-controller/spec.md)
**Input**: Feature specification from `/specs/006-refactor-permission-controller/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

主要需求是重構 `PermissionController` 中的權限列表相關API端點，使其回傳 `PagedApiResponseModel<PermissionDto>`，並重構 `PermissionService` 中的權限列表相關方法，使其回傳 `PageResultDto<PermissionDto>`，同時確保現有非分頁API不受影響。此技術方法將嚴格遵循憲章中的第九原則 (Pagination Architecture & Layer Responsibility) 和第八原則 (Controller Response DTO Architecture)，以確保分層職責分離、資料庫層級分頁執行、一致的回應格式及API層的測試性。

## Technical Context

**Language/Version**: C# 13  
**Primary Dependencies**: ASP.NET Core, Dapper, FluentValidation  
**Storage**: PostgreSQL  
**Testing**: Unit tests, Integration tests (涵蓋API端點、分頁邏輯、資料映射)  
**Target Platform**: Linux server  
**Project Type**: Web API  
**Performance Goals**: 99% 的有效分頁請求能在500毫秒內獲得響應 (來自SC-002)。  
**Constraints**:
- 分頁參數 (pageNumber, pageSize) 必須經過驗證。
- JWT 令牌應在一小時內過期。
- 敏感資訊（如密碼）不得記錄或在錯誤訊息中洩露。
- 輸入必須通過 FluentValidation 驗證。  
**Scale/Scope**:
- 此次重構的範圍限於 `PermissionController` 和 `PermissionService` 中涉及權限列表分頁的API端點和相關服務方法。
- **NEEDS CLARIFICATION**: 權限的最大數量或權限列表的典型資料量預期為何？ (這將影響分頁查詢的實作細節和效能考量)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality Excellence
- **狀態**: 合規。重構將嚴格遵循C# 13最佳實踐、XML文件、命名慣例和可空參考型別的使用。
### II. Three-Layer Architecture Compliance
- **狀態**: 合規。此次重構旨在加強並明確控制器、服務和儲存層之間的職責分離。
### III. Database Design & Foreign Key Integrity
- **狀態**: 不適用。本次重構不涉及資料庫結構變更，但將確保現有資料庫查詢符合效率要求。
### IV. Permission-Based Authorization Design
- **狀態**: 合規。`PermissionController` 的重構將維持並遵循既有的基於權限的授權設計模式。
### V. Test-First Development
- **狀態**: 合規。憲章要求在實作前編寫測試 (SC-003)，本次重構將遵守此原則。
### VI. User Experience Consistency & Admin Interface Standards
- **狀態**: 合規。`PagedApiResponseModel` 的導入將確保分頁API回應的一致性，並遵守回應格式標準。
### VII. Performance & Security Standards for Account Management
- **狀態**: 合規。將根據效能目標 (500毫秒回應時間) 最佳化分頁查詢，並維持既有的安全標準。
### VIII. Controller Response DTO Architecture
- **狀態**: 合規。這是重構的核心目標之一，將確保控制器回傳專用的回應DTO，避免服務層DTO直接暴露。
### IX. Pagination Architecture & Layer Responsibility
- **狀態**: 合規。這是重構的主要驅動原則，將嚴格按照此原則實作分頁邏輯和分層職責。

## Project Structure

### Documentation (this feature)

```text
specs/006-refactor-permission-controller/
├── plan.md              # 此文件 (/speckit.plan 命令輸出)
├── research.md          # 階段 0 輸出 (/speckit.plan 命令)
├── data-model.md        # 階段 1 輸出 (/speckit.plan 命令)
├── quickstart.md        # 階段 1 輸出 (/speckit.plan 命令)
├── contracts/           # 階段 1 輸出 (/speckit.plan 命令)
└── tasks.md             # 階段 2 輸出 (/speckit.tasks 命令 - 非由 /speckit.plan 創建)
```

### Source Code (repository root)

```text
V3.Admin.Backend/
├── Configuration/
├── Controllers/
│   └── PermissionController.cs  # 重構目標
├── Database/
├── Middleware/
├── Models/
│   ├── ApiResponseModel.cs
│   ├── ResponseCodes.cs
│   ├── Dtos/
│   │   └── PermissionDto.cs
│   ├── Entities/
│   ├── Requests/
│   └── Responses/
│       └── PagedResultDto.cs  # 需確保存在或創建
├── Repositories/
│   └── PermissionRepository.cs # 可能需要調整以支援分頁
├── Services/
│   └── PermissionService.cs   # 重構目標
├── Tests/
│   ├── Integration/          # 需增加整合測試
│   └── Unit/                 # 需增加單元測試
└── Validators/
```

**Structure Decision**: 採用現有的單一專案結構，重點在於`V3.Admin.Backend`專案內`Controllers`、`Services`及`Models/Responses`資料夾的修改與擴展。將確保`PagedResultDto.cs`的存在，並可能調整`PermissionRepository.cs`以支援分頁查詢。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*無違規事項，無需填寫此部分。*