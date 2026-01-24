using BCrypt.Net;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 身份驗證服務實作
/// </summary>
/// <remarks>
/// 負責處理使用者登入驗證相關的業務邏輯
/// </remarks>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="userRepository">使用者資料存取層</param>
    /// <param name="jwtService">JWT 服務</param>
    /// <param name="logger">日誌記錄器</param>
    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="loginDto">登入資訊</param>
    /// <returns>登入結果,包含 JWT Token 與使用者資訊</returns>
    /// <exception cref="UnauthorizedAccessException">帳號或密碼錯誤</exception>
    /// <exception cref="InvalidOperationException">帳號已被刪除</exception>
    public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
    {
        // 查詢使用者 (不區分大小寫)
        var user = await _userRepository.GetByUsernameAsync(loginDto.Account.ToLowerInvariant());

        // 帳號不存在或密碼錯誤
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "登入失敗: 帳號 {Account} 的憑證無效",
                loginDto.Account
            );
            throw new UnauthorizedAccessException("帳號或密碼錯誤");
        }

        // 帳號已被刪除
        if (user.IsDeleted)
        {
            _logger.LogWarning(
                "登入失敗: 帳號 {Account} 已被刪除",
                loginDto.Account
            );
            throw new UnauthorizedAccessException("帳號或密碼錯誤");
        }

        // 產生 JWT Token
        string token = _jwtService.GenerateToken(user);
        DateTimeOffset expiresAt = _jwtService.GetTokenExpirationTime();

        _logger.LogInformation(
            "使用者 {Account} (ID: {UserId}) 登入成功",
            user.Account,
            user.Id
        );

        // 返回登入結果
        return new LoginResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new AccountDto
            {
                Id = user.Id,
                Account = user.Account,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Version = user.Version
            }
        };
    }
}
