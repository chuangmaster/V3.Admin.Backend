using System.Data;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 簽名記錄資料存取介面
/// </summary>
public interface ISignatureRecordRepository
{
    /// <summary>
    /// 建立簽名記錄
    /// </summary>
    /// <param name="record">簽名記錄實體</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的簽名記錄</returns>
    Task<SignatureRecord> CreateAsync(
        SignatureRecord record,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得服務單的簽名記錄清單
    /// </summary>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>簽名記錄清單</returns>
    Task<List<SignatureRecord>> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}
