using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 使用者資料存取介面
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根據 ID 查詢使用者
    /// </summary>
    /// <param name="id">使用者 ID</param>
    /// <returns>使用者實體,若不存在或已刪除則回傳 null</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// 根據帳號名稱查詢使用者 (不區分大小寫)
    /// </summary>
    /// <param name="username">帳號名稱</param>
    /// <returns>使用者實體,若不存在或已刪除則回傳 null</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// 檢查帳號是否存在 (不區分大小寫)
    /// </summary>
    /// <param name="username">帳號名稱</param>
    /// <returns>true: 帳號存在, false: 帳號不存在</returns>
    Task<bool> ExistsAsync(string username);

    /// <summary>
    /// 分頁查詢所有使用者
    /// </summary>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <returns>使用者清單</returns>
    Task<IEnumerable<User>> GetAllAsync(int pageNumber, int pageSize);

    /// <summary>
    /// 建立新使用者
    /// </summary>
    /// <param name="user">使用者實體</param>
    /// <returns>true: 建立成功, false: 建立失敗</returns>
    Task<bool> CreateAsync(User user);

    /// <summary>
    /// 更新使用者 (使用樂觀並發控制)
    /// </summary>
    /// <param name="user">使用者實體</param>
    /// <param name="expectedVersion">預期版本號</param>
    /// <returns>true: 更新成功, false: 版本衝突或使用者不存在</returns>
    Task<bool> UpdateAsync(User user, int expectedVersion);

    /// <summary>
    /// 軟刪除使用者
    /// </summary>
    /// <param name="id">使用者 ID</param>
    /// <param name="operatorId">執行刪除的操作者 ID</param>
    /// <returns>true: 刪除成功, false: 使用者不存在或已刪除</returns>
    Task<bool> DeleteAsync(Guid id, Guid operatorId);

    /// <summary>
    /// 計算有效帳號數量 (排除已刪除)
    /// </summary>
    /// <returns>有效帳號數量</returns>
    Task<int> CountActiveAsync();
}
