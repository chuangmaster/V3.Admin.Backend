using System.Data;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using V3.Admin.Backend.Configuration;
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

        // ===== 組態設定 =====
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
        JwtSettings? jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

        // ===== 資料庫連接 =====
        builder.Services.AddScoped<IDbConnection>(sp =>
            new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

        // ===== Repositories =====
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        // ===== Services =====
        builder.Services.AddScoped<IJwtService, JwtService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAccountService, AccountService>();

        // ===== FluentValidation =====
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        // ===== JWT Authentication =====
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? string.Empty)
                    )
                };
            });

        builder.Services.AddAuthorization();

        // ===== Controllers =====
        builder.Services.AddControllers();

        // ===== Swagger/OpenAPI =====
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "V3 Admin Backend - 帳號管理 API",
                Version = "v1.0",
                Description = "V3 管理後台帳號管理系統 API",
                Contact = new OpenApiContact
                {
                    Name = "V3 Admin Backend Team"
                }
            });

            // JWT Bearer 認證
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "使用 JWT Bearer Token 進行身份驗證。在下方輸入: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // XML 文件註解 (Phase 7 Polish 階段啟用)
            // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // options.IncludeXmlComments(xmlPath);
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

        // 身份驗證與授權
        app.UseAuthentication();
        app.UseAuthorization();

        // Controllers
        app.MapControllers();

        app.Run();
    }
}

// Make Program class accessible to test project
public partial class Program { }
