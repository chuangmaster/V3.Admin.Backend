using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// JWT 服務介面
/// </summary>
/// <remarks>
/// 負責 JWT Token 的產生與驗證
/// </remarks>
public interface IJwtService
{
    /// <summary>
    /// 產生 JWT Token
    /// </summary>
    /// <param name="user">使用者實體</param>
    /// <returns>JWT Token 字串</returns>
    string GenerateToken(User user);

    /// <summary>
    /// 取得 Token 過期時間
    /// </summary>
    /// <returns>過期時間 (UTC)</returns>
    DateTimeOffset GetTokenExpirationTime();
}
