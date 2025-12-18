using System.Data;
using Dapper;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 簽名記錄資料存取實作
/// </summary>
public class SignatureRecordRepository : ISignatureRecordRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<SignatureRecordRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public SignatureRecordRepository(IDbConnection dbConnection, ILogger<SignatureRecordRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 建立簽名記錄
    /// </summary>
    public async Task<SignatureRecord> CreateAsync(
        SignatureRecord record,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            INSERT INTO signature_records (
                id, service_order_id, document_type, signature_type,
                signature_data,
                dropbox_sign_request_id, dropbox_sign_status, dropbox_sign_url,
                signer_name, signed_at,
                created_at, created_by
            )
            VALUES (
                @Id, @ServiceOrderId, @DocumentType, @SignatureType,
                @SignatureData,
                @DropboxSignRequestId, @DropboxSignStatus, @DropboxSignUrl,
                @SignerName, @SignedAt,
                @CreatedAt, @CreatedBy
            )
            RETURNING *;
        ";

        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.NewGuid();
        }

        if (record.CreatedAt == default)
        {
            record.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            SignatureRecord result = await _dbConnection.QuerySingleAsync<SignatureRecord>(
                sql,
                record,
                transaction: transaction
            );
            _logger.LogInformation("簽名記錄已建立: {Id} ({DocumentType})", result.Id, result.DocumentType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立簽名記錄失敗: ServiceOrderId={ServiceOrderId}", record.ServiceOrderId);
            throw;
        }
    }

    /// <summary>
    /// 取得服務單的簽名記錄清單
    /// </summary>
    public async Task<List<SignatureRecord>> GetByServiceOrderIdAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            SELECT *
            FROM signature_records
            WHERE service_order_id = @ServiceOrderId
            ORDER BY created_at ASC;
        ";

        return (await _dbConnection.QueryAsync<SignatureRecord>(sql, new { ServiceOrderId = serviceOrderId }))
            .ToList();
    }
}
