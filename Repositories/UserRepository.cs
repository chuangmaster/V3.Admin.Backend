using System.Data;
using Dapper;
using Npgsql;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Repositories.Interfaces;

namespace V3.Admin.Backend.Repositories;

/// <summary>
/// 使用者資料存取實作
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="connection">資料庫連線</param>
    /// <param name="logger">日誌記錄器</param>
    public UserRepository(IDbConnection connection, ILogger<UserRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id, account, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users WHERE id = @Id AND is_deleted = false";

        return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT id, account, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users WHERE LOWER(account) = LOWER(@Account) AND is_deleted = false";

        return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Account = username });
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string username)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE LOWER(account) = LOWER(@Account) AND is_deleted = false)";
        return await _connection.ExecuteScalarAsync<bool>(sql, new { Account = username });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await SearchAsync(pageNumber, pageSize, null);
    }

    /// <summary>
    /// 使用 searchKeyword 搜尋帳號 (分頁)
    /// </summary>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁數量</param>
    /// <param name="searchKeyword">搜尋關鍵字 (比對 account 和 display_name，不區分大小寫)</param>
    /// <returns>符合條件的帳號清單</returns>
    public async Task<IEnumerable<User>> SearchAsync(int pageNumber, int pageSize, string? searchKeyword)
    {
        const string sql = @"
            SELECT id, account, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users 
            WHERE is_deleted = false
            AND (@SearchKeyword IS NULL OR LOWER(account) LIKE LOWER(@SearchKeyword) OR LOWER(display_name) LIKE LOWER(@SearchKeyword))
            ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";

        int offset = (pageNumber - 1) * pageSize;
        var searchKeywordPattern = string.IsNullOrWhiteSpace(searchKeyword) ? null : $"%{searchKeyword}%";
        return await _connection.QueryAsync<User>(sql, new { SearchKeyword = searchKeywordPattern, PageSize = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<bool> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, account, password_hash, display_name, created_at, is_deleted, version)
            VALUES (@Id, @Account, @PasswordHash, @DisplayName, @CreatedAt, @IsDeleted, @Version)";

        try
        {
            int affected = await _connection.ExecuteAsync(sql, user);
            return affected > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(ex, "建立使用者失敗: 帳號 {Account} 已有重複鍵", user.Account);
            throw new InvalidOperationException($"帳號 {user.Account} 已存在");
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(User user, int expectedVersion)
    {
        const string sql = @"
            UPDATE users SET display_name = @DisplayName, password_hash = @PasswordHash,
                   updated_at = @UpdatedAt, version = version + 1
            WHERE id = @Id AND version = @ExpectedVersion AND is_deleted = false";

        int affected = await _connection.ExecuteAsync(sql, new
        {
            user.DisplayName,
            user.PasswordHash,
            UpdatedAt = DateTime.UtcNow,
            user.Id,
            ExpectedVersion = expectedVersion
        });

        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, Guid operatorId)
    {
        const string sql = @"
            UPDATE users SET is_deleted = true, deleted_at = @DeletedAt, deleted_by = @DeletedBy
            WHERE id = @Id AND is_deleted = false";

        int affected = await _connection.ExecuteAsync(sql, new
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = operatorId
        });

        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<int> CountActiveAsync()
    {
        return await CountAsync(null);
    }

    /// <summary>
    /// 搜尋符合 searchKeyword 的有效帳號總數
    /// </summary>
    /// <param name="searchKeyword">搜尋關鍵字 (比對 account 和 display_name，不區分大小寫)</param>
    /// <returns>符合條件的帳號總數</returns>
    public async Task<int> CountAsync(string? searchKeyword)
    {
        const string sql = @"
            SELECT COUNT(*) FROM users 
            WHERE is_deleted = false
            AND (@SearchKeyword IS NULL OR LOWER(account) LIKE LOWER(@SearchKeyword) OR LOWER(display_name) LIKE LOWER(@SearchKeyword))";

        var searchKeywordPattern = string.IsNullOrWhiteSpace(searchKeyword) ? null : $"%{searchKeyword}%";
        return await _connection.ExecuteScalarAsync<int>(sql, new { SearchKeyword = searchKeywordPattern });
    }
}
