using System.Data;
using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 商品項目資料存取介面
/// </summary>
public interface IProductItemRepository
{
    /// <summary>
    /// 批次建立商品項目
    /// </summary>
    /// <param name="items">商品項目清單</param>
    /// <param name="transaction">資料庫交易 (選填)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否建立成功</returns>
    Task<bool> BatchCreateAsync(
        IReadOnlyCollection<ProductItem> items,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 取得服務單的商品項目清單
    /// </summary>
    /// <param name="serviceOrderId">服務單 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>商品項目清單</returns>
    Task<List<ProductItem>> GetByServiceOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}
