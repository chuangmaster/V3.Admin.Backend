# API 契約

此重構任務**不會改變公開的 API JSON 契約**。主要目標是重構**內部實作**以符合專案的 constitution (`Principle VIII`)。

- 來自 `PermissionController` 的回應 JSON 結構應與先前版本保持一致，以避免對 API 消費者造成破壞性變更。
- 更改是在 C# 型別層面：`ApiResponseModel<List<PermissionDto>>` 變為 `ApiResponseModel<List<PermissionResponse>>`。只要序列化的 JSON 欄位名稱相同，這就不是一個破壞性變更。