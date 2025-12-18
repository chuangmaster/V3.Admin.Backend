using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 服務單資料存取實作
/// </summary>
public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<ServiceOrderRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public ServiceOrderRepository(IDbConnection dbConnection, ILogger<ServiceOrderRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 建立服務單
    /// </summary>
    /// <remarks>
    /// 若 sequence_number 為 0,會由資料庫觸發器自動填入當日序號
    /// </remarks>
    public async Task<ServiceOrder> CreateAsync(
        ServiceOrder serviceOrder,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            INSERT INTO service_orders (
                id, service_date, sequence_number, order_type, order_source,
                customer_id, total_amount, status,
                consignment_start_date, consignment_end_date, renewal_option,
                created_at, created_by,
                is_deleted, version
            )
            VALUES (
                @Id, @ServiceDate, @SequenceNumber, @OrderType, @OrderSource,
                @CustomerId, @TotalAmount, @Status,
                @ConsignmentStartDate, @ConsignmentEndDate, @RenewalOption,
                @CreatedAt, @CreatedBy,
                false, 1
            )
            RETURNING *;
        ";

        if (serviceOrder.Id == Guid.Empty)
        {
            serviceOrder.Id = Guid.NewGuid();
        }

        if (serviceOrder.ServiceDate == default)
        {
            serviceOrder.ServiceDate = DateTime.UtcNow.Date;
        }

        if (serviceOrder.CreatedAt == default)
        {
            serviceOrder.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            ServiceOrder result = await _dbConnection.QuerySingleAsync<ServiceOrder>(
                sql,
                serviceOrder,
                transaction: transaction
            );
            _logger.LogInformation("服務單已建立: {OrderNumber} ({Id})", result.OrderNumber, result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立服務單失敗: CustomerId={CustomerId}", serviceOrder.CustomerId);
            throw;
        }
    }

    /// <summary>
    /// 根據 ID 取得服務單
    /// </summary>
    public async Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM service_orders
            WHERE id = @Id AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<ServiceOrder?>(sql, new { Id = id });
    }

    /// <summary>
    /// 根據服務單編號取得服務單
    /// </summary>
    public async Task<ServiceOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM service_orders
            WHERE order_number = @OrderNumber AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<ServiceOrder?>(sql, new { OrderNumber = orderNumber });
    }

    /// <summary>
    /// 更新服務單狀態 (樂觀鎖)
    /// </summary>
    public async Task<bool> UpdateStatusAsync(
        Guid id,
        string newStatus,
        Guid operatorId,
        int expectedVersion,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            UPDATE service_orders
            SET status = @NewStatus,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy,
                version = version + 1
            WHERE id = @Id AND version = @ExpectedVersion AND is_deleted = false
            RETURNING 1;
        ";

        try
        {
            int? result = await _dbConnection.QuerySingleOrDefaultAsync<int?>(
                sql,
                new
                {
                    Id = id,
                    NewStatus = newStatus,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = operatorId,
                    ExpectedVersion = expectedVersion,
                },
                transaction: transaction
            );

            return result is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新服務單狀態失敗: {Id}", id);
            throw;
        }
    }
}
