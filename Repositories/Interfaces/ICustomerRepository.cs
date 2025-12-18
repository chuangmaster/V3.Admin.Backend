using System.Data;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 客戶資料存取介面
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// 建立客戶
    /// </summary>
    /// <param name="customer">客戶實體</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的客戶</returns>
    Task<Customer> CreateAsync(
        Customer customer,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根據 ID 取得客戶
    /// </summary>
    /// <param name="id">客戶 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>客戶實體，若不存在或已刪除則回傳 null</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據身分證字號取得客戶 (不區分大小寫)
    /// </summary>
    /// <param name="idNumber">身分證字號/外籍人士格式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>客戶實體，若不存在或已刪除則回傳 null</returns>
    Task<Customer?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 依條件搜尋客戶 (分頁)
    /// </summary>
    /// <remarks>
    /// 支援姓名/Email 模糊搜尋 (ILIKE)、電話/身分證字號精確搜尋
    /// </remarks>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="name">姓名 (模糊搜尋)</param>
    /// <param name="phoneNumber">電話 (精確搜尋)</param>
    /// <param name="email">Email (模糊搜尋)</param>
    /// <param name="idNumber">身分證字號 (精確搜尋)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合條件的客戶清單與總筆數</returns>
    Task<(List<Customer> Items, int TotalCount)> SearchAsync(
        int pageNumber,
        int pageSize,
        string? name,
        string? phoneNumber,
        string? email,
        string? idNumber,
        CancellationToken cancellationToken = default
    );
}
