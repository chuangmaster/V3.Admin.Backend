using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using Serilog.Context;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Converters;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Repositories;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend;

public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // ===== Serilog 日誌配置 (使用 UTC 時間戳記) =====
        builder.Host.UseSerilog(
            (context, services, logger) =>
                logger
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "V3.Admin.Backend")
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .WriteTo.Console(
                        outputTemplate: "[{UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        "logs/v3-admin-backend-.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        );

        // ===== 組態設定 =====
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection(JwtSettings.SectionName)
        );

        // ===== Dapper 配置 =====
        // 設定 Dapper 的命名規則轉換 (snake_case <-> PascalCase)
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // ===== 資料庫連接 (設定 UTC 時區) =====
        builder.Services.AddScoped<IDbConnection>(sp =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // 確保連線字串包含 Timezone=UTC 參數
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timezone = "UTC"
            };
            
            return new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        });

        // ===== Repositories =====
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();
        builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        builder.Services.AddScoped<
            IPermissionFailureLogRepository,
            PermissionFailureLogRepository
        >();
        builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // ===== Services =====
        builder.Services.AddScoped<IJwtService, JwtService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();
        builder.Services.AddScoped<IPermissionValidationService, PermissionValidationService>();
        builder.Services.AddScoped<IAuditLogService, AuditLogService>();

        // ===== FluentValidation =====
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        // ===== Distributed Cache =====
        builder.Services.AddDistributedMemoryCache();

        // ===== JWT Authentication =====
        builder
            .Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // 直接從 Configuration 在此處讀取 JwtSettings，確保測試時由 CustomWebApplicationFactory 注入的設定會被使用
                var cfg = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = cfg?.Issuer,
                    ValidAudience = cfg?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(cfg?.SecretKey ?? string.Empty)
                    ),
                };

                // 設定 JWT Bearer 事件以記錄驗證過程
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<
                            ILogger<Program>
                        >();
                        logger.LogWarning(
                            "JWT 驗證失敗 | Exception: {Exception} | TraceId: {TraceId}",
                            context.Exception.Message,
                            context.HttpContext.TraceIdentifier
                        );
                        // Debug: also write to console to ensure test runner captures this
                        Console.WriteLine($"[JWT DEBUG] Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<
                            ILogger<Program>
                        >();
                        var claims = string.Join(
                            "; ",
                            context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? []
                        );
                        logger.LogInformation(
                            "JWT 驗證成功 | Claims: {Claims} | TraceId: {TraceId}",
                            claims,
                            context.HttpContext.TraceIdentifier
                        );
                        // Debug: also write to console so tests can see token claims
                        Console.WriteLine($"[JWT DEBUG] Token validated. Claims: {claims}");
                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddAuthorization();

        // ===== Controllers (配置 JSON 序列化) =====
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // 註冊 UTC0 時間轉換器
                options.JsonSerializerOptions.Converters.Add(new Utc0DateTimeJsonConverter());
                
                // 其他 JSON 設定
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
            });

        // ===== Swagger/OpenAPI =====
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "V3 Admin Backend - 帳號管理 API",
                    Version = "v1.0",
                    Description = "V3 管理後台帳號管理系統 API",
                    Contact = new OpenApiContact { Name = "V3 Admin Backend Team" },
                }
            );

            // JWT Bearer 認證
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description = "使用 JWT Bearer Token 進行身份驗證。在下方輸入: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                }
            );

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        Array.Empty<string>()
                    },
                }
            );

            // 載入 XML 文件註解
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        WebApplication app = builder.Build();

        // ===== Middleware Pipeline =====

        // 全域異常處理 (必須在最前面)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // TraceId 注入
        app.UseMiddleware<TraceIdMiddleware>();

        // HTTPS 重定向
        app.UseHttpsRedirection();

        // Swagger UI (開發環境)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "V3 Admin Backend API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // 身份驗證與授權 (必須在權限驗證之前)
        app.UseAuthentication();

        // JWT Token 版本驗證 (確保密碼修改後舊 Token 失效)
        app.UseMiddleware<VersionValidationMiddleware>();

        app.UseAuthorization();

        // 權限授權驗證 (必須在 UseAuthentication 之後)
        app.UseMiddleware<PermissionAuthorizationMiddleware>();

        // Controllers
        app.MapControllers();

        app.Run();
    }
}

// Make Program class accessible to test project
public partial class Program { }
