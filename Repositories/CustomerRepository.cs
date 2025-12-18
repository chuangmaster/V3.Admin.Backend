using System.Data;
using Dapper;
using Npgsql;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 客戶資料存取實作
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<CustomerRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public CustomerRepository(IDbConnection dbConnection, ILogger<CustomerRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    /// <summary>
    /// 建立客戶
    /// </summary>
    public async Task<Customer> CreateAsync(
        Customer customer,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = @"
            INSERT INTO customers (
                id, name, phone_number, email, id_number,
                created_at, created_by,
                is_deleted, version
            )
            VALUES (
                @Id, @Name, @PhoneNumber, @Email, @IdNumber,
                @CreatedAt, @CreatedBy,
                false, 1
            )
            RETURNING *;
        ";

        if (customer.Id == Guid.Empty)
        {
            customer.Id = Guid.NewGuid();
        }

        if (customer.CreatedAt == default)
        {
            customer.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            Customer result = await _dbConnection.QuerySingleAsync<Customer>(
                sql,
                customer,
                transaction: transaction
            );
            _logger.LogInformation("客戶已建立: {CustomerId} ({IdNumber})", result.Id, result.IdNumber);
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(ex, "建立客戶失敗: 身分證字號 {IdNumber} 已存在", customer.IdNumber);
            throw new InvalidOperationException("身分證字號已存在,不可重複建立");
        }
    }

    /// <summary>
    /// 根據 ID 取得客戶
    /// </summary>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM customers
            WHERE id = @Id AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<Customer?>(sql, new { Id = id });
    }

    /// <summary>
    /// 根據身分證字號取得客戶
    /// </summary>
    public async Task<Customer?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM customers
            WHERE UPPER(id_number) = UPPER(@IdNumber) AND is_deleted = false;
        ";

        return await _dbConnection.QuerySingleOrDefaultAsync<Customer?>(sql, new { IdNumber = idNumber });
    }

    /// <summary>
    /// 依條件搜尋客戶 (分頁)
    /// </summary>
    public async Task<(List<Customer> Items, int TotalCount)> SearchAsync(
        int pageNumber,
        int pageSize,
        string? name,
        string? phoneNumber,
        string? email,
        string? idNumber,
        CancellationToken cancellationToken = default
    )
    {
        var whereClauses = new List<string> { "is_deleted = false" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("name ILIKE @Name");
            parameters.Add("@Name", $"%{name}%");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            whereClauses.Add("phone_number = @PhoneNumber");
            parameters.Add("@PhoneNumber", phoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            whereClauses.Add("email ILIKE @Email");
            parameters.Add("@Email", $"%{email}%");
        }

        if (!string.IsNullOrWhiteSpace(idNumber))
        {
            whereClauses.Add("UPPER(id_number) = UPPER(@IdNumber)");
            parameters.Add("@IdNumber", idNumber);
        }

        int offset = (pageNumber - 1) * pageSize;
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", pageSize);

        string whereClause = string.Join(" AND ", whereClauses);

        string countSql = $"SELECT COUNT(*) FROM customers WHERE {whereClause};";
        string dataSql = $@"
            SELECT *
            FROM customers
            WHERE {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        List<Customer> items = (await _dbConnection.QueryAsync<Customer>(dataSql, parameters)).ToList();

        return (items, totalCount);
    }
}
