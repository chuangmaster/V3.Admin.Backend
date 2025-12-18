using System.Data;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 服務單資料存取介面
/// </summary>
public interface IServiceOrderRepository
{
    /// <summary>
    /// 建立服務單
    /// </summary>
    /// <param name="serviceOrder">服務單實體</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的服務單</returns>
    Task<ServiceOrder> CreateAsync(
        ServiceOrder serviceOrder,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根據 ID 取得服務單
    /// </summary>
    /// <param name="id">服務單 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服務單實體，若不存在或已刪除則回傳 null</returns>
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據服務單編號取得服務單
    /// </summary>
    /// <param name="orderNumber">服務單編號 (BS/CS + YYYYMMDD + 001)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服務單實體，若不存在或已刪除則回傳 null</returns>
    Task<ServiceOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新服務單狀態 (使用樂觀並發控制)
    /// </summary>
    /// <param name="id">服務單 ID</param>
    /// <param name="newStatus">新狀態</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="expectedVersion">預期版本號</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true: 更新成功, false: 版本衝突或服務單不存在</returns>
    Task<bool> UpdateStatusAsync(
        Guid id,
        string newStatus,
        Guid operatorId,
        int expectedVersion,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );
}
