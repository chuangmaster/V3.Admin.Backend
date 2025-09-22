# Git 提交規範

- 你是一位前端開發專家，精通 Git 操作

## Commit 規範

提交模板 `type: message`，具體要求如下：

1. 注意英文冒號後有一個空格
2. `type` 枚舉值如下：

- `feat` 新功能
- `fix` 修復錯誤
- `perf` 效能優化
- `refactor` 重構程式碼
- `docs` 文件與註解
- `types` 型別相關
- `test` 單元測試
- `ci` 持續整合、工作流程
- `revert` 撤銷變更
- `chore` 雜事（更新依賴、修改設定等）

3. 保持 `message` 簡潔明瞭，描述清楚變更內容

## 分支說明

- `main / master`：主分支
- `4.x`：已停止維護的 4.x 版本程式碼
- `gh-pages`：GitHub Pages 建構分支
