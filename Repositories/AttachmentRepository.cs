using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 附件資料存取實作
/// </summary>
public class AttachmentRepository : IAttachmentRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<AttachmentRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public AttachmentRepository(IDbConnection dbConnection, ILogger<AttachmentRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 建立附件
    /// </summary>
    public async Task<Attachment> CreateAsync(
        Attachment attachment,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            INSERT INTO attachments (
                id, service_order_id, attachment_type,
                file_name, blob_path, file_size, content_type,
                created_at, created_by,
                is_deleted
            )
            VALUES (
                @Id, @ServiceOrderId, @AttachmentType,
                @FileName, @BlobPath, @FileSize, @ContentType,
                @CreatedAt, @CreatedBy,
                false
            )
            RETURNING *;
        ";

        if (attachment.Id == Guid.Empty)
        {
            attachment.Id = Guid.NewGuid();
        }

        if (attachment.CreatedAt == default)
        {
            attachment.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            Attachment result = await _dbConnection.QuerySingleAsync<Attachment>(
                sql,
                attachment,
                transaction: transaction
            );
            _logger.LogInformation("附件已建立: {AttachmentId} ({AttachmentType})", result.Id, result.AttachmentType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立附件失敗: ServiceOrderId={ServiceOrderId}", attachment.ServiceOrderId);
            throw;
        }
    }

    /// <summary>
    /// 取得服務單的附件清單
    /// </summary>
    public async Task<List<Attachment>> GetByServiceOrderIdAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            SELECT *
            FROM attachments
            WHERE service_order_id = @ServiceOrderId AND is_deleted = false
            ORDER BY created_at ASC;
        ";

        return (await _dbConnection.QueryAsync<Attachment>(sql, new { ServiceOrderId = serviceOrderId }))
            .ToList();
    }

    /// <summary>
    /// 軟刪除附件
    /// </summary>
    public async Task<bool> SoftDeleteAsync(
        Guid id,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            UPDATE attachments
            SET is_deleted = true,
                deleted_at = @DeletedAt,
                deleted_by = @DeletedBy
            WHERE id = @Id AND is_deleted = false
            RETURNING 1;
        ";

        try
        {
            int? result = await _dbConnection.QuerySingleOrDefaultAsync<int?>(
                sql,
                new
                {
                    Id = id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = operatorId,
                }
            );

            return result is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "軟刪除附件失敗: {Id}", id);
            throw;
        }
    }
}
