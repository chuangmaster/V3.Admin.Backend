# V3.Admin.Backend - 後台管理系統後端

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-46_Passing-success)](Tests/)

[English](README_EN.md) | 繁體中文

現代化的後台管理系統後端 API，基於 ASP.NET Core 10 與 PostgreSQL 構建。本專案配合 [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) 前端專案，快速打造完整的企業級後台管理系統，提供帳號認證、權限管理、角色控制等核心功能。

## 🎯 專案特色

本專案是配合 **[V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite)** 前端框架設計的後端系統，可快速搭建具備以下特色的企業級後台：

- 🚀 **開箱即用** - 完整的前後端整合方案，快速啟動專案開發
- 🎨 **現代化前端** - Vue 3 + TypeScript + Element Plus 管理介面
- ⚡ **高效能後端** - .NET 10 + PostgreSQL 提供穩定高效的 API 服務
- 🔐 **完整權限系統** - RBAC 角色權限控制，細粒度權限管理

## ✨ 功能特色

- 🔐 **JWT 身份驗證** - 基於 Bearer Token 的無狀態身份驗證
- 👤 **帳號管理** - 完整的 CRUD 操作 (新增、查詢、更新、刪除)
- 🔑 **密碼管理** - BCrypt 雜湊 (work factor 12) + 密碼變更
- 🛡️ **安全性** - 輸入驗證、SQL 注入防護、軟刪除機制
- 🔄 **並發控制** - 樂觀鎖定 (Optimistic Locking) 防止資料衝突
- 📝 **完整日誌** - 結構化日誌記錄與 TraceId 追蹤
- 📚 **API 文件** - 內建 Swagger UI 互動式文件
- ✅ **高測試覆蓋** - 42 個單元測試 + 4 個整合測試 (100% 通過)
- 🌐 **繁體中文** - 完整繁體中文錯誤訊息與文件
- 🐳 **Docker 支援** - 容器化部署就緒

## � 相關專案

- **前端專案**: [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) - Vue 3 + TypeScript + Element Plus 管理後台

本專案提供完整的 RESTful API，可與前端專案無縫整合，快速構建企業級後台管理系統。

## �🚀 快速開始

### 前置需求

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (選用,用於整合測試)

### 安裝步驟

1. **複製專案**
   ```powershell
   git clone https://github.com/your-org/V3.Admin.Backend.git
   cd V3.Admin.Backend
   git checkout 001-account-management
   ```

2. **設定資料庫**
   ```powershell
   # 建立資料庫
   psql -U postgres -c "CREATE DATABASE v3admin_dev;"
   
   # 執行遷移
   cd Database/Migrations
   Get-ChildItem -Filter "*.sql" | Sort-Object Name | ForEach-Object {
       psql -U postgres -d v3admin_dev -f $_.FullName
   }
   cd ../..
   
   # 插入測試資料
   psql -U postgres -d v3admin_dev -f Database/Scripts/seed.sql
   ```

3. **設定組態**
   
   編輯 `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=v3admin_dev;Username=postgres;Password=postgres"
     },
     "JwtSettings": {
       "SecretKey": "YourSecretKeyAtLeast32Characters!!!",
       "Issuer": "V3.Admin.Backend",
       "Audience": "V3.Admin.Frontend",
       "ExpirationMinutes": 60
     }
   }
   ```

4. **啟動應用程式**
   ```powershell
   dotnet run
   ```
   
   瀏覽器開啟 `https://localhost:5001/swagger`

### 預設測試帳號

- 帳號: `admin` / 密碼: `Admin@123`
- 帳號: `testuser` / 密碼: `Test@123`

## 📖 API 端點

### 身份驗證

| 方法 | 端點 | 說明 | 授權 |
|------|------|------|------|
| POST | `/api/auth/login` | 使用者登入 | ❌ |

### 帳號管理

| 方法 | 端點 | 說明 | 授權 |
|------|------|------|------|
| GET | `/api/accounts` | 查詢帳號列表 (分頁) | ✅ |
| GET | `/api/accounts/{id}` | 查詢單一帳號 | ✅ |
| POST | `/api/accounts` | 新增帳號 | ✅ |
| PUT | `/api/accounts/{id}` | 更新帳號資訊 | ✅ |
| PUT | `/api/accounts/{id}/password` | 變更密碼 | ✅ |
| DELETE | `/api/accounts/{id}` | 刪除帳號 (軟刪除) | ✅ |

### API 使用範例

#### 登入
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

#### 新增帳號
```bash
curl -X POST https://localhost:5001/api/accounts \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"username":"newuser","password":"Secure@123","displayName":"新使用者"}'
```

完整的 API 文件與範例請參閱 [Swagger UI](https://localhost:5001/swagger) 或 [Quickstart Guide](specs/001-account-management/quickstart.md)。

## 🏗️ 技術架構

### 技術堆疊

- **框架**: ASP.NET Core 10.0 (Web API)
- **語言**: C# 14
- **資料庫**: PostgreSQL 15+
- **ORM**: Dapper (Micro-ORM)
- **身份驗證**: JWT Bearer Token
- **密碼雜湊**: BCrypt.Net-Next
- **輸入驗證**: FluentValidation
- **API 文件**: Swagger/OpenAPI
- **測試框架**: xUnit + Moq + FluentAssertions + Testcontainers

### 專案結構

```
V3.Admin.Backend/
├── Controllers/          # API 控制器
│   ├── AuthController.cs
│   ├── AccountController.cs
│   └── BaseApiController.cs
├── Services/             # 業務邏輯層
│   ├── AuthService.cs
│   ├── AccountService.cs
│   └── JwtService.cs
├── Repositories/         # 資料存取層
│   └── UserRepository.cs
├── Models/               # 資料模型
│   ├── Entities/         # 資料庫實體
│   ├── Dtos/             # 資料傳輸物件
│   ├── Requests/         # API 請求模型
│   └── Responses/        # API 回應模型
├── Validators/           # FluentValidation 驗證器
├── Middleware/           # 中介軟體
│   ├── ExceptionHandlingMiddleware.cs
│   └── TraceIdMiddleware.cs
├── Configuration/        # 組態模型
├── Database/             # 資料庫腳本
│   ├── Migrations/       # 遷移腳本
│   └── Scripts/          # 種子資料
└── Tests/                # 測試專案
    ├── Unit/             # 單元測試 (42 tests)
    └── Integration/      # 整合測試 (4 tests)
```

### 架構設計

- **三層架構**: Controller → Service → Repository
- **依賴注入**: 使用 ASP.NET Core DI 容器
- **DTO 模式**: 分離內部模型與 API 合約
- **Repository 模式**: 抽象資料存取邏輯
- **中介軟體管道**: 集中處理例外與 TraceId
- **統一回應格式**: `ApiResponseModel<T>` 包裝所有回應

### API 回應格式

所有 API 回應遵循統一格式:

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "操作成功",
  "data": { ... },
  "timestamp": "2025-10-28T10:30:00Z",
  "traceId": "7d3e5f8a-2b4c-4d9e-8f7a-1c2d3e4f5a6b"
}
```

HTTP 狀態碼與業務代碼對照:

| HTTP | 業務代碼 | 說明 |
|------|---------|------|
| 200 | SUCCESS | 操作成功 |
| 201 | CREATED | 資源建立成功 |
| 400 | VALIDATION_ERROR | 輸入驗證錯誤 |
| 401 | UNAUTHORIZED / INVALID_CREDENTIALS | 未授權 / 憑證錯誤 |
| 404 | NOT_FOUND | 資源不存在 |
| 409 | CONCURRENT_UPDATE_CONFLICT | 並發更新衝突 |
| 422 | USERNAME_EXISTS / ... | 業務邏輯錯誤 |
| 500 | INTERNAL_ERROR | 系統內部錯誤 |

## 🧪 測試

### 執行測試

```powershell
# 執行所有測試 (46 tests)
dotnet test

# 僅執行單元測試 (42 tests)
dotnet test --filter "FullyQualifiedName!~Integration"

# 僅執行整合測試 (4 tests, 需要 Docker)
dotnet test --filter "FullyQualifiedName~Integration"

# 詳細輸出
dotnet test --logger "console;verbosity=detailed"
```

### 測試覆蓋率

| 類別 | 測試數 | 狀態 |
|-----|--------|------|
| Validators (LoginRequest) | 7 | ✅ |
| Validators (CreateAccountRequest) | 7 | ✅ |
| Validators (UpdateAccountRequest) | 6 | ✅ |
| Validators (ChangePasswordRequest) | 6 | ✅ |
| Validators (DeleteAccountRequest) | 2 | ✅ |
| Services (AuthService) | 4 | ✅ |
| Integration (AuthController) | 4 | ✅ |
| **總計** | **46** | **✅ 100%** |

### 整合測試

整合測試使用 **Testcontainers** 自動啟動 PostgreSQL 容器,無需手動設定測試資料庫:

```powershell
# 確保 Docker Desktop 正在執行
docker ps

# 執行整合測試 (自動建立 PostgreSQL 容器)
dotnet test --filter "FullyQualifiedName~Integration"
```

## 📋 開發指南

### 編碼規範

- 遵循 [C# 編碼慣例](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 XML 註解文件化所有 public 成員
- 所有錯誤訊息使用繁體中文
- 使用 `async/await` 進行非同步操作
- Repository 方法必須有對應的單元測試

### 新增功能

1. 從 `main` 分支建立新 feature branch
2. 實作功能並撰寫測試 (測試優先開發建議)
3. 確保所有測試通過 (`dotnet test`)
4. 更新 API 文件 (Swagger 註解)
5. 提交 Pull Request

### Git 工作流程

```powershell
# 建立功能分支
git checkout -b feature/your-feature-name

# 提交變更
git add .
git commit -m "feat: 新增 XXX 功能"

# 推送到遠端
git push origin feature/your-feature-name
```

### Commit 訊息格式

遵循 [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` 新功能
- `fix:` 錯誤修復
- `docs:` 文件更新
- `test:` 測試相關
- `refactor:` 重構
- `perf:` 效能改進
- `chore:` 雜項任務

## 🔒 安全性

### 已實作的安全措施

- ✅ JWT Bearer Token 身份驗證
- ✅ BCrypt 密碼雜湊 (work factor 12)
- ✅ SQL 注入防護 (參數化查詢)
- ✅ 輸入驗證 (FluentValidation)
- ✅ 軟刪除機制 (資料審計)
- ✅ 樂觀鎖定 (防止並發衝突)
- ✅ HTTPS 強制
- ✅ 統一錯誤處理 (避免資訊洩漏)

### 安全建議

- 🔐 **生產環境 JWT SecretKey** 必須儲存於環境變數或 Azure Key Vault
- 🔐 **資料庫連線字串** 不應包含在原始碼中
- 🔐 **啟用 HTTPS** 並使用有效憑證
- 🔐 **設定 CORS** 限制允許的來源
- 🔐 **實作速率限制** 防止暴力破解
- 🔐 **定期更新** 相依套件以修補安全漏洞

## 📚 文件

- **[快速入門指南](specs/001-account-management/quickstart.md)** - 完整的安裝與使用教學
- **[功能規格](specs/001-account-management/spec.md)** - 使用者故事與驗收條件
- **[實作計畫](specs/001-account-management/plan.md)** - 64 項任務清單
- **[API 規格](specs/001-account-management/contracts/api-spec.yaml)** - OpenAPI 3.0 規格
- **[Swagger UI](https://localhost:5001/swagger)** - 互動式 API 文件 (需啟動應用程式)

## 🤝 貢獻

歡迎貢獻！本專案與 [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) 前端專案共同維護，致力於提供最佳的全端後台解決方案。

請遵循以下步驟:

1. Fork 本專案
2. 建立 feature branch (`git checkout -b feature/amazing-feature`)
3. 提交變更 (`git commit -m 'feat: 新增某功能'`)
4. 推送至分支 (`git push origin feature/amazing-feature`)
5. 開啟 Pull Request

請確保:
- 所有測試通過
- 新增功能有對應測試
- 遵循專案編碼規範
- 更新相關文件

## 📝 版本歷史

### v1.0.0 (2025-10-28)

**功能**:
- ✅ JWT 身份驗證系統
- ✅ 帳號新增、查詢、更新、刪除
- ✅ 密碼變更功能
- ✅ 軟刪除機制
- ✅ 樂觀鎖定並發控制
- ✅ 完整的輸入驗證
- ✅ 46 個測試 (100% 通過)
- ✅ Swagger API 文件
- ✅ Docker 支援

**已知限制**:
- 不支援角色權限管理 (計畫於 v2.0 實作)
- 不支援密碼重設 Email (計畫於 v2.0 實作)
- 不支援兩階段驗證 (計畫於 v3.0 實作)

## 📄 授權

本專案採用 [MIT License](LICENSE) 授權 - 詳見 LICENSE 檔案


## 🌟 相關資源

- [V3 Admin Vite (前端)](https://github.com/chuangmaster/v3-admin-vite) - Vue 3 管理後台前端專案
- [線上文件](https://github.com/chuangmaster/V3.Admin.Backend/wiki) - 詳細的開發文件
- [問題回報](https://github.com/chuangmaster/V3.Admin.Backend/issues) - 回報問題或功能建議

---

⭐ 如果這個專案對您有幫助，請給我們一個 Star！同時也歡迎查看配套的[前端專案](https://github.com/chuangmaster/v3-admin-vite)！
