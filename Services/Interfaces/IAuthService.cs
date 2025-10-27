using V3.Admin.Backend.Models.Dtos;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 身份驗證服務介面
/// </summary>
/// <remarks>
/// 負責處理使用者登入驗證相關的業務邏輯
/// </remarks>
public interface IAuthService
{
    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="loginDto">登入資訊</param>
    /// <returns>登入結果,包含 JWT Token 與使用者資訊</returns>
    /// <exception cref="UnauthorizedAccessException">帳號或密碼錯誤</exception>
    /// <exception cref="InvalidOperationException">帳號已被刪除</exception>
    Task<LoginResultDto> LoginAsync(LoginDto loginDto);
}
