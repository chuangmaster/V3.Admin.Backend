using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace V3.Admin.Backend.Tests.Helpers;

/// <summary>
/// 資料庫測試基礎設施，使用 Testcontainers 提供 PostgreSQL 測試環境
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // 執行資料庫遷移腳本
        await ExecuteMigrationScripts();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    private async Task ExecuteMigrationScripts()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        // 建立 users 表格
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                username VARCHAR(20) NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                display_name VARCHAR(100) NOT NULL,
                version INTEGER NOT NULL DEFAULT 1,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                deleted_at TIMESTAMP WITH TIME ZONE,
                deleted_by UUID
            );

            CREATE INDEX IF NOT EXISTS idx_users_username ON users(username) WHERE is_deleted = FALSE;
            CREATE INDEX IF NOT EXISTS idx_users_is_deleted ON users(is_deleted);
        ";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
