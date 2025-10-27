using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 身份驗證控制器
/// </summary>
/// <remarks>
/// 負責處理登入相關的 HTTP 請求
/// </remarks>
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="authService">身份驗證服務</param>
    /// <param name="logger">日誌記錄器</param>
    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入結果,包含 JWT Token 與使用者資訊</returns>
    /// <response code="200">登入成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">帳號或密碼錯誤</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseModel<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // 轉換為 DTO
            var loginDto = new LoginDto
            {
                Username = request.Username,
                Password = request.Password
            };

            // 執行登入
            var result = await _authService.LoginAsync(loginDto);

            // 轉換為 Response
            var response = new LoginResponse
            {
                Token = result.Token,
                ExpiresAt = result.ExpiresAt,
                User = new AccountResponse
                {
                    Id = result.User.Id,
                    Username = result.User.Username,
                    DisplayName = result.User.DisplayName,
                    CreatedAt = result.User.CreatedAt,
                    UpdatedAt = result.User.UpdatedAt
                }
            };

            return Success(response, "登入成功");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "登入失敗: {Message}", ex.Message);
            return UnauthorizedResponse(ex.Message, ResponseCodes.INVALID_CREDENTIALS);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "登入失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.INVALID_CREDENTIALS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登入時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }
}
