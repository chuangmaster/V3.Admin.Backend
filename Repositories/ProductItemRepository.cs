using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 商品項目資料存取實作
/// </summary>
public class ProductItemRepository : IProductItemRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<ProductItemRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public ProductItemRepository(IDbConnection dbConnection, ILogger<ProductItemRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 批次建立商品項目
    /// </summary>
    public async Task<bool> BatchCreateAsync(
        IReadOnlyCollection<ProductItem> items,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            INSERT INTO product_items (
                id, service_order_id, sequence_number,
                brand_name, style_name, internal_code,
                accessories, defects,
                created_at, updated_at
            )
            VALUES (
                @Id, @ServiceOrderId, @SequenceNumber,
                @BrandName, @StyleName, @InternalCode,
                COALESCE(@Accessories::jsonb, NULL),
                COALESCE(@Defects::jsonb, NULL),
                @CreatedAt, @UpdatedAt
            );
        ";

        if (items is null || items.Count == 0)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;
        foreach (ProductItem item in items)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            if (item.CreatedAt == default)
            {
                item.CreatedAt = now;
            }

            item.UpdatedAt ??= now;
        }

        try
        {
            int affected = await _dbConnection.ExecuteAsync(sql, items, transaction: transaction);
            return affected == items.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次建立商品項目失敗: Count={Count}", items.Count);
            throw;
        }
    }

    /// <summary>
    /// 取得服務單的商品項目清單
    /// </summary>
    public async Task<List<ProductItem>> GetByServiceOrderIdAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            SELECT *
            FROM product_items
            WHERE service_order_id = @ServiceOrderId
            ORDER BY sequence_number ASC;
        ";

        return (await _dbConnection.QueryAsync<ProductItem>(sql, new { ServiceOrderId = serviceOrderId }))
            .ToList();
    }
}
