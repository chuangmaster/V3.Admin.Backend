# 快速入門指南 - 帳號管理系統

**Feature**: 001-account-management  
**Date**: 2025-10-26  
**Target Audience**: 後端開發人員、前端整合人員、系統管理員

## 目錄

1. [環境需求](#1-環境需求)
2. [本機開發設定](#2-本機開發設定)
3. [資料庫設定](#3-資料庫設定)
4. [執行應用程式](#4-執行應用程式)
5. [API 使用範例](#5-api-使用範例)
6. [測試](#6-測試)
7. [常見問題](#7-常見問題)
8. [下一步](#8-下一步)

---

## 1. 環境需求

### 必要軟體

- **.NET SDK**: 9.0 或更新版本
  - 下載: https://dotnet.microsoft.com/download
  - 驗證安裝: `dotnet --version`

- **PostgreSQL**: 15 或更新版本
  - 下載: https://www.postgresql.org/download/
  - 或使用 Docker: `docker run --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15`

- **IDE** (選擇一項):
  - Visual Studio 2022 (17.8+)
  - Visual Studio Code + C# Dev Kit
  - JetBrains Rider

### 選用工具

- **Docker Desktop**: 用於容器化部署
- **Postman** 或 **Insomnia**: API 測試工具
- **pgAdmin** 或 **DBeaver**: PostgreSQL 圖形化管理工具

---

## 2. 本機開發設定

### 2.1 複製專案

```powershell
# 複製儲存庫
git clone https://github.com/your-org/V3.Admin.Backend.git
cd V3.Admin.Backend

# 切換到功能分支
git checkout 001-account-management
```

### 2.2 安裝相依套件

```powershell
# 還原 NuGet 套件
dotnet restore

# 驗證建置
dotnet build
```

### 2.3 設定應用程式組態

編輯 `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=v3admin_dev;Username=postgres;Password=postgres;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100"
  },
  "Jwt": {
    "SecretKey": "YourDevelopmentSecretKeyAtLeast32Characters!",
    "Issuer": "V3.Admin.Backend",
    "Audience": "V3.Admin.Frontend",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**注意**: 
- 生產環境 `SecretKey` 必須使用強密鑰並儲存於環境變數或 Key Vault
- 連線字串應根據本機 PostgreSQL 設定調整

---

## 3. 資料庫設定

### 3.1 建立資料庫

```sql
-- 使用 psql 或 pgAdmin 執行
CREATE DATABASE v3admin_dev
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;
```

或使用 PowerShell:

```powershell
# 使用 psql 命令列工具
$env:PGPASSWORD = "postgres"
psql -U postgres -h localhost -c "CREATE DATABASE v3admin_dev;"
```

### 3.2 執行資料庫遷移

```powershell
# 進入 Database/Migrations 目錄
cd Database/Migrations

# 執行遷移腳本
$env:PGPASSWORD = "postgres"
Get-ChildItem -Filter "*.sql" | Sort-Object Name | ForEach-Object {
    Write-Host "執行遷移: $($_.Name)"
    psql -U postgres -h localhost -d v3admin_dev -f $_.FullName
}

# 返回專案根目錄
cd ../..
```

### 3.3 插入初始資料

```powershell
# 執行 seed 腳本
$env:PGPASSWORD = "postgres"
psql -U postgres -h localhost -d v3admin_dev -f Database/Scripts/seed.sql
```

**預設帳號**:
- 帳號: `admin` / 密碼: `Admin@123`
- 帳號: `testuser` / 密碼: `Test@123`

### 3.4 驗證資料庫設定

```sql
-- 檢查資料表
\dt

-- 檢查使用者資料
SELECT id, username, display_name, created_at, is_deleted FROM users;
```

---

## 4. 執行應用程式

### 4.1 開發模式

```powershell
# 啟動應用程式 (自動重新載入)
dotnet watch run

# 或使用 IDE 的偵錯功能 (F5)
```

應用程式將在以下位址啟動:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### 4.2 生產模式

```powershell
# 編譯發行版本
dotnet publish -c Release -o ./publish

# 執行發行版本
cd publish
dotnet V3.Admin.Backend.dll
```

### 4.3 Docker 容器化

```powershell
# 建置 Docker 映像
docker build -t v3-admin-backend:latest .

# 執行容器
docker run -d `
  --name v3-admin-backend `
  -p 8080:80 `
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=v3admin_dev;Username=postgres;Password=postgres" `
  -e Jwt__SecretKey="YourProductionSecretKeyAtLeast32Characters!" `
  v3-admin-backend:latest
```

### 4.4 驗證應用程式啟動

開啟瀏覽器訪問:
- Swagger UI: `https://localhost:5001/swagger`
- Health Check: `https://localhost:5001/health` (若有實作)

---

## 5. API 使用範例

### 5.1 使用 cURL

#### 登入

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin@123"
  }'
```

**回應範例**:
```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "登入成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-10-26T15:30:00Z",
    "user": {
      "id": "00000000-0000-0000-0000-000000000001",
      "username": "admin",
      "displayName": "系統管理員",
      "createdAt": "2025-10-26T08:00:00Z",
      "updatedAt": null
    }
  },
  "timestamp": "2025-10-26T14:30:00Z",
  "traceId": "7d3e5f8a-2b4c-4d9e-8f7a-1c2d3e4f5a6b"
}
```

#### 新增帳號

```bash
# 將 <TOKEN> 替換為登入回傳的 token
curl -X POST https://localhost:5001/api/accounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "username": "newuser",
    "password": "SecureP@ss123",
    "displayName": "新使用者"
  }'
```

#### 查詢帳號列表

```bash
curl -X GET "https://localhost:5001/api/accounts?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer <TOKEN>"
```

#### 更新個人資訊

```bash
curl -X PUT https://localhost:5001/api/accounts/<USER_ID> \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "displayName": "新的顯示名稱"
  }'
```

#### 變更密碼

```bash
curl -X PUT https://localhost:5001/api/accounts/<USER_ID>/password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "oldPassword": "Admin@123",
    "newPassword": "NewSecureP@ss456"
  }'
```

#### 刪除帳號

```bash
curl -X DELETE https://localhost:5001/api/accounts/<USER_ID> \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "confirmation": "CONFIRM"
  }'
```

---

### 5.2 使用 PowerShell

#### 登入

```powershell
$loginResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    username = "admin"
    password = "Admin@123"
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$token = $loginResponse.data.token
Write-Host "Token: $token"
```

#### 新增帳號

```powershell
$headers = @{
  Authorization = "Bearer $token"
  "Content-Type" = "application/json"
}

$createResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/accounts" `
  -Method Post `
  -Headers $headers `
  -Body (@{
    username = "newuser"
    password = "SecureP@ss123"
    displayName = "新使用者"
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$createResponse.data
```

#### 查詢帳號列表

```powershell
$listResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/accounts?pageNumber=1&pageSize=10" `
  -Method Get `
  -Headers @{ Authorization = "Bearer $token" } `
  -SkipCertificateCheck

$listResponse.data.items | Format-Table
```

---

### 5.3 使用 Swagger UI

1. 開啟瀏覽器訪問 `https://localhost:5001/swagger`
2. 點擊 `POST /api/auth/login` 展開
3. 點擊 "Try it out"
4. 輸入登入憑證:
   ```json
   {
     "username": "admin",
     "password": "Admin@123"
   }
   ```
5. 點擊 "Execute"
6. 複製回應中的 `data.token`
7. 點擊頁面上方的 "Authorize" 按鈕
8. 輸入 `Bearer <TOKEN>` (注意 Bearer 後有空格)
9. 點擊 "Authorize" 確認
10. 現在可以測試其他需要身份驗證的端點

---

## 6. 測試

### 6.1 執行單元測試

```powershell
# 執行所有單元測試
dotnet test --filter "Category=Unit"

# 執行特定測試類別
dotnet test --filter "FullyQualifiedName~AccountServiceTests"

# 顯示詳細輸出
dotnet test --logger "console;verbosity=detailed"
```

### 6.2 執行整合測試

```powershell
# 需要 Testcontainers (自動啟動 PostgreSQL 容器)
dotnet test --filter "Category=Integration"
```

**注意**: 整合測試需要 Docker Desktop 執行。

### 6.3 測試覆蓋率

```powershell
# 安裝覆蓋率工具
dotnet tool install --global dotnet-reportgenerator-globaltool

# 執行測試並收集覆蓋率
dotnet test --collect:"XPlat Code Coverage"

# 生成覆蓋率報告
reportgenerator `
  -reports:"**/coverage.cobertura.xml" `
  -targetdir:"coveragereport" `
  -reporttypes:Html

# 開啟報告
Start-Process "coveragereport/index.html"
```

### 6.4 手動測試清單

使用 Swagger UI 或 Postman 執行以下測試場景:

- [ ] **登入成功**: 使用正確的帳號密碼
- [ ] **登入失敗**: 使用錯誤的密碼
- [ ] **新增帳號**: 建立新帳號
- [ ] **帳號重複**: 嘗試建立已存在的帳號 (應失敗)
- [ ] **更新資訊**: 修改顯示名稱
- [ ] **變更密碼**: 變更密碼並使用新密碼登入
- [ ] **密碼相同**: 嘗試將新密碼設為與舊密碼相同 (應失敗)
- [ ] **刪除帳號**: 刪除帳號並嘗試登入 (應失敗)
- [ ] **刪除自己**: 嘗試刪除當前登入帳號 (應失敗)
- [ ] **最後帳號**: 嘗試刪除最後一個帳號 (應失敗)
- [ ] **未授權**: 不帶 Token 訪問受保護端點 (應 401)
- [ ] **Token 過期**: 使用過期 Token (應 401)

---

## 7. 常見問題

### Q1: 無法連接到 PostgreSQL

**問題**: `Npgsql.NpgsqlException: 無法連接到伺服器`

**解決方案**:
1. 確認 PostgreSQL 服務已啟動:
   ```powershell
   # Windows
   Get-Service postgresql*
   
   # 或檢查 Docker 容器
   docker ps | Select-String postgres
   ```
2. 檢查連線字串中的 Host、Port、Username、Password
3. 確認防火牆允許 PostgreSQL 連接埠 (預設 5432)
4. 使用 `psql` 測試連接:
   ```powershell
   psql -U postgres -h localhost
   ```

---

### Q2: JWT Token 無效或過期

**問題**: API 回傳 `401 Unauthorized`

**解決方案**:
1. 確認 Token 是否過期 (有效期 1 小時)
2. 重新登入取得新 Token
3. 確認 Authorization 標頭格式: `Bearer <token>` (注意空格)
4. 檢查 `appsettings.json` 中的 `Jwt:SecretKey` 是否一致

---

### Q3: 密碼驗證失敗

**問題**: 登入時密碼正確但仍然失敗

**解決方案**:
1. 確認資料庫中的密碼雜湊是否使用 BCrypt
2. 檢查密碼是否包含特殊字元需要轉義
3. 使用 seed.sql 中的預設帳號測試
4. 查看日誌中的詳細錯誤訊息

---

### Q4: 資料庫遷移失敗

**問題**: 執行遷移腳本時出現錯誤

**解決方案**:
1. 確認資料庫是否已存在: `psql -l`
2. 刪除並重新建立資料庫:
   ```sql
   DROP DATABASE IF EXISTS v3admin_dev;
   CREATE DATABASE v3admin_dev;
   ```
3. 確認遷移腳本執行順序 (依檔名排序)
4. 檢查 PostgreSQL 版本是否支援所有語法 (需 15+)

---

### Q5: Swagger UI 無法載入

**問題**: 訪問 `/swagger` 時出現 404

**解決方案**:
1. 確認應用程式在開發模式執行 (`ASPNETCORE_ENVIRONMENT=Development`)
2. 檢查 `Program.cs` 是否註冊 Swagger:
   ```csharp
   builder.Services.AddSwaggerGen();
   app.UseSwagger();
   app.UseSwaggerUI();
   ```
3. 確認應用程式啟動訊息中包含 Swagger 端點

---

### Q6: 測試失敗

**問題**: 單元測試或整合測試失敗

**解決方案**:
1. 清理並重新建置:
   ```powershell
   dotnet clean
   dotnet build
   ```
2. 確認 Docker Desktop 已啟動 (整合測試需要)
3. 檢查測試輸出的詳細錯誤訊息:
   ```powershell
   dotnet test --logger "console;verbosity=detailed"
   ```
4. 確認測試資料庫與開發資料庫隔離

---

## 8. 下一步

### 8.1 前端整合

前端開發人員可參考:
- **API 規格**: [contracts/api-spec.yaml](./contracts/api-spec.yaml)
- **Swagger 文件**: `https://localhost:5001/swagger`
- **範例程式碼**: 本文件第 5 節

### 8.2 功能擴充

未來可能的功能擴充:
- 角色與權限管理 (RBAC)
- 審計日誌查詢介面
- 密碼重設 (Email/SMS)
- 兩階段驗證 (2FA)
- OAuth 2.0 整合 (Google/Microsoft)
- 帳號停用/啟用
- 批次匯入/匯出帳號

### 8.3 生產部署

準備部署至生產環境時:
1. 閱讀 `research.md` 中的「部署與環境設定」章節
2. 設定環境變數 (不使用 appsettings.json 儲存敏感資訊)
3. 使用 Azure Key Vault 或類似服務管理密鑰
4. 設定 HTTPS 憑證
5. 設定日誌記錄與監控 (Application Insights/ELK)
6. 實作健康檢查端點
7. 設定 CI/CD 管道 (Azure DevOps/GitHub Actions)

### 8.4 學習資源

- **ASP.NET Core 文件**: https://learn.microsoft.com/aspnet/core/
- **Dapper 教學**: https://github.com/DapperLib/Dapper
- **PostgreSQL 文件**: https://www.postgresql.org/docs/
- **JWT 標準**: https://datatracker.ietf.org/doc/html/rfc7519
- **BCrypt 說明**: https://en.wikipedia.org/wiki/Bcrypt

---

## 支援

如有問題或需要協助:
- 查看專案 Wiki
- 提交 GitHub Issue
- 聯絡團隊成員

**版本**: 1.0.0 | **最後更新**: 2025-10-26
