using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;
using V3.Admin.Backend.Tests.Helpers;

namespace V3.Admin.Backend.Tests.Integration;

/// <summary>
/// 自定義 WebApplicationFactory，用於整合測試
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private DatabaseFixture? _databaseFixture;

    public string ConnectionString => _databaseFixture?.ConnectionString ?? string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 移除現有的設定
            config.Sources.Clear();

            // 加入測試用的設定
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:ConnectionString"] = ConnectionString,
                ["JwtSettings:SecretKey"] = "test-secret-key-for-integration-tests-minimum-32-characters",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:ExpirationHours"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 移除原本註冊的 IDbConnection
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbConnection));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 註冊測試用的資料庫連線
            services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(ConnectionString));
        });
    }

    public async Task InitializeAsync()
    {
        _databaseFixture = new DatabaseFixture();
        await _databaseFixture.InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_databaseFixture != null)
        {
            await _databaseFixture.DisposeAsync();
        }
    }
}
