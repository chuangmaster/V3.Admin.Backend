# 階段 1：快速入門指南

本指南概述了如何驗證 `PermissionController` 重構的成功實施。

## 先決條件

1.  應用程式正在本地運行。
2.  您擁有管理員的有效 JWT 令牌，具有與權限相關的存取權。
3.  可以使用 `curl`、`Postman` 或內建的 `.http` 檔案執行器等工具。

## 驗證步驟

### 1. 驗證 `GET /api/permissions` 端點

此端點應返回所有權限的列表，以標準的 `ApiResponseModel` 包裝，並為每個項目使用新的 `PermissionResponse` DTO。

**請求：**

```http
GET http://localhost:5000/api/permissions
Authorization: Bearer <YOUR_JWT_TOKEN>
```

**預期成功回應 (200 OK)：**

`data` 欄位應包含與 `PermissionResponse` 結構相符的物件列表。

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "查詢成功",
  "data": [
    {
      "permissionId": 1,
      "permissionCode": "account.create",
      "name": "新增帳號",
      "description": "允許新增使用者帳號"
    },
    {
      "permissionId": 2,
      "permissionCode": "account.read",
      "name": "查詢帳號",
      "description": "允許查詢使用者帳號"
    }
    // ... 其他權限
  ],
  "timestamp": "...",
  "traceId": "..."
}
```

### 2. 驗證程式碼實作

- **開啟 `Controllers/PermissionController.cs`**：
  - 確認沒有方法直接返回 `PermissionDto` 或 `List<PermissionDto>`。
  - 確認所有 `return` 語句都使用 `BaseController` 輔助方法，例如 `Success(...)`。
  - 確認在 Controller 方法內部，有明確的程式碼將 `PermissionDto` 映射到 `PermissionResponse`。
- **開啟 `Models/Responses/PermissionResponse.cs`**：
  - 確認此檔案存在，並且在其屬性、建構函式或方法中沒有引用 `PermissionDto` 或任何其他服務層 DTO。

透過遵循這些步驟，您可以確認重構符合功能規格和專案 constitution 中定義的要求。
