using System.Data;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 附件資料存取介面
/// </summary>
public interface IAttachmentRepository
{
    /// <summary>
    /// 建立附件
    /// </summary>
    /// <param name="attachment">附件實體</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的附件</returns>
    Task<Attachment> CreateAsync(
        Attachment attachment,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得服務單的附件清單 (排除已刪除)
    /// </summary>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>附件清單</returns>
    Task<List<Attachment>> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 軟刪除附件
    /// </summary>
    /// <param name="id">附件 ID</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> SoftDeleteAsync(Guid id, Guid operatorId, CancellationToken cancellationToken = default);
}
