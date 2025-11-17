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
            SELECT id, username, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users WHERE id = @Id AND is_deleted = false";

        return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT id, username, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users WHERE LOWER(username) = LOWER(@Username) AND is_deleted = false";

        return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string username)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE LOWER(username) = LOWER(@Username) AND is_deleted = false)";
        return await _connection.ExecuteScalarAsync<bool>(sql, new { Username = username });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
            SELECT id, username, password_hash AS PasswordHash, display_name AS DisplayName, 
                   created_at AS CreatedAt, updated_at AS UpdatedAt, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, deleted_by AS DeletedBy, version
            FROM users WHERE is_deleted = false ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";

        int offset = (pageNumber - 1) * pageSize;
        return await _connection.QueryAsync<User>(sql, new { PageSize = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<bool> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, username, password_hash, display_name, created_at, is_deleted, version)
            VALUES (@Id, @Username, @PasswordHash, @DisplayName, @CreatedAt, @IsDeleted, @Version)";

        try
        {
            int affected = await _connection.ExecuteAsync(sql, user);
            return affected > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(ex, "建立使用者失敗: 帳號 {Username} 已有重複鍵", user.Username);
            throw new InvalidOperationException($"帳號 {user.Username} 已存在");
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
        const string sql = "SELECT COUNT(*) FROM users WHERE is_deleted = false";
        return await _connection.ExecuteScalarAsync<int>(sql);
    }
}
