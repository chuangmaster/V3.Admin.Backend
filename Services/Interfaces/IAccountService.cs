using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 帳號管理服務介面
/// </summary>
/// <remarks>
/// 負責處理帳號管理相關的業務邏輯
/// </remarks>
public interface IAccountService
{
    /// <summary>
    /// 新增帳號
    /// </summary>
    /// <param name="dto">新增帳號資訊</param>
    /// <returns>新建立的帳號資訊</returns>
    /// <exception cref="InvalidOperationException">帳號已存在</exception>
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto);

    /// <summary>
    /// 更新帳號資訊
    /// </summary>
    /// <param name="dto">更新帳號資訊</param>
    /// <returns>更新後的帳號資訊</returns>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="InvalidOperationException">並發更新衝突</exception>
    Task<AccountDto> UpdateAccountAsync(UpdateAccountDto dto);

    /// <summary>
    /// 變更密碼
    /// </summary>
    /// <param name="dto">變更密碼資訊</param>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="UnauthorizedAccessException">舊密碼錯誤</exception>
    /// <exception cref="InvalidOperationException">新密碼與舊密碼相同或並發更新衝突</exception>
    Task ChangePasswordAsync(ChangePasswordDto dto);

    /// <summary>
    /// 查詢單一帳號
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <returns>帳號資訊</returns>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    Task<AccountDto> GetAccountByIdAsync(Guid id);

    /// <summary>
    /// 查詢帳號列表 (分頁)
    /// </summary>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁數量</param>
    /// <returns>帳號列表</returns>
    Task<AccountListDto> GetAccountsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// 刪除帳號 (軟刪除)
    /// </summary>
    /// <param name="id">要刪除的帳號 ID</param>
    /// <param name="operatorId">執行刪除操作的使用者 ID</param>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="InvalidOperationException">無法刪除當前登入帳號或最後一個有效帳號</exception>
    Task DeleteAccountAsync(Guid id, Guid operatorId);
}
