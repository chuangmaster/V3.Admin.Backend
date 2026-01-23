using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// JWT 服務實作
/// </summary>
/// <remarks>
/// 使用 HMAC-SHA256 簽章產生 JWT Token
/// </remarks>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 產生 JWT Token
    /// </summary>
    /// <remarks>
    /// Token 包含 user_id、version、account 等 claims。
    /// version claim 用於實作 Token 強制失效機制：
    /// 當使用者密碼被修改時，version 會遞增，
    /// 使得舊 Token 中的 version 與資料庫不匹配而被拒絕。
    /// </remarks>
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("user_id", user.Id.ToString()),
            new Claim("version", user.Version.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Account),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: GetTokenExpirationTime(),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "JWT Token 已產生 | UserId: {UserId} | Account: {Account} | ExpiresAt: {ExpiresAt}",
            user.Id, user.Account, GetTokenExpirationTime());

        return tokenString;
    }

    /// <summary>
    /// 取得 Token 過期時間
    /// </summary>
    public DateTime GetTokenExpirationTime()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
    }
}
