# 階段 1：資料模型

此功能著重於重構 API 回應，而非更改核心資料庫架構。此處的資料模型代表將在 API 回應中公開的 **資料傳輸物件 (DTO)**。

## 1. PermissionResponse

在 API 回應中表示單一權限實體。此 DTO 是一個純粹的資料容器，不包含業務邏輯。

| Field          | Type   | Description (說明)       | Constraints (限制) | Example                  |
|----------------|--------|--------------------------|--------------------|--------------------------|
| `PermissionId` | `int`  | 權限的唯一識別碼         | Required, > 0      | `101`                    |
| `PermissionCode`| `string`| 權限的程式碼 (e.g., `role.read`) | Required, Not empty | `"role.read"`            |
| `Name`         | `string`| 權限的顯示名稱 (中文)    | Required, Not empty | `"查詢角色"`             |
| `Description`  | `string`| 權限的詳細描述           | Optional           | `"允許使用者查看角色列表"`|

## 2. PermissionListResponse

Represents a list of permissions, typically for endpoints that return multiple permissions.

| Field   | Type                     | Description (說明) | Constraints (限制) |
|---------|--------------------------|--------------------|--------------------|
| `Items` | `List<PermissionResponse>`| 權限物件列表       | Required, not null |
