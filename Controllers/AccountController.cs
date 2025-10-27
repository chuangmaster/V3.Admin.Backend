using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 帳號管理控制器
/// </summary>
/// <remarks>
/// 負責處理帳號管理相關的 HTTP 請求 (需要身份驗證)
/// </remarks>
[Route("api/[controller]")]
[Authorize]
public class AccountController : BaseApiController
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="accountService">帳號管理服務</param>
    /// <param name="logger">日誌記錄器</param>
    public AccountController(
        IAccountService accountService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢帳號列表
    /// </summary>
    /// <param name="pageNumber">頁碼 (預設 1)</param>
    /// <param name="pageSize">每頁數量 (預設 10)</param>
    /// <returns>帳號列表</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="401">未授權</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseModel<AccountListResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    public async Task<IActionResult> GetAccounts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            AccountListDto result = await _accountService.GetAccountsAsync(pageNumber, pageSize);

            AccountListResponse response = new AccountListResponse
            {
                Items = result.Items.Select(dto => new AccountResponse
                {
                    Id = dto.Id,
                    Username = dto.Username,
                    DisplayName = dto.DisplayName,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                }).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
            };

            return Success(response, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢帳號列表時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 查詢單一帳號
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <returns>帳號資訊</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="401">未授權</response>
    /// <response code="404">帳號不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        try
        {
            AccountDto result = await _accountService.GetAccountByIdAsync(id);

            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Username = result.Username,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            return Success(response, "查詢成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "查詢帳號失敗: {Message}", ex.Message);
            return NotFound("帳號不存在", ResponseCodes.NOT_FOUND);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢帳號時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 新增帳號
    /// </summary>
    /// <param name="request">新增帳號請求</param>
    /// <returns>新建立的帳號資訊</returns>
    /// <response code="201">帳號建立成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">未授權</response>
    /// <response code="422">帳號已存在</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 422)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            // 轉換為 DTO
            CreateAccountDto createDto = new CreateAccountDto
            {
                Username = request.Username,
                Password = request.Password,
                DisplayName = request.DisplayName
            };

            // 執行新增
            AccountDto result = await _accountService.CreateAccountAsync(createDto);

            // 轉換為 Response
            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Username = result.Username,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            return Created(response, "帳號建立成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "新增帳號失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.USERNAME_EXISTS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增帳號時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 更新帳號資訊
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <param name="request">更新帳號請求</param>
    /// <param name="version">版本號 (用於樂觀並發控制)</param>
    /// <returns>更新後的帳號資訊</returns>
    /// <response code="200">更新成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">未授權</response>
    /// <response code="404">帳號不存在</response>
    /// <response code="409">並發更新衝突</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 409)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request, [FromQuery] int version = 1)
    {
        try
        {
            // 轉換為 DTO
            UpdateAccountDto updateDto = new UpdateAccountDto
            {
                Id = id,
                DisplayName = request.DisplayName,
                Version = version
            };

            // 執行更新
            AccountDto result = await _accountService.UpdateAccountAsync(updateDto);

            // 轉換為 Response
            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Username = result.Username,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            return Success(response, "更新成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "更新帳號失敗: {Message}", ex.Message);
            return NotFound("帳號不存在", ResponseCodes.NOT_FOUND);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("並發") || ex.Message.Contains("衝突"))
        {
            _logger.LogWarning(ex, "更新帳號失敗: {Message}", ex.Message);
            return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新帳號時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 變更密碼
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <param name="request">變更密碼請求</param>
    /// <param name="version">版本號 (用於樂觀並發控制)</param>
    /// <returns>無內容</returns>
    /// <response code="200">變更成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">未授權或舊密碼錯誤</response>
    /// <response code="404">帳號不存在</response>
    /// <response code="409">並發更新衝突</response>
    /// <response code="422">新密碼與舊密碼相同</response>
    [HttpPut("{id}/password")]
    [ProducesResponseType(typeof(ApiResponseModel), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 409)]
    [ProducesResponseType(typeof(ApiResponseModel), 422)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, [FromQuery] int version = 1)
    {
        try
        {
            // 轉換為 DTO
            ChangePasswordDto changeDto = new ChangePasswordDto
            {
                Id = id,
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword,
                Version = version
            };

            // 執行變更密碼
            await _accountService.ChangePasswordAsync(changeDto);

            return Success("密碼變更成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "變更密碼失敗: {Message}", ex.Message);
            return NotFound("帳號不存在", ResponseCodes.NOT_FOUND);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "變更密碼失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.INVALID_CREDENTIALS);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("相同"))
        {
            _logger.LogWarning(ex, "變更密碼失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.PASSWORD_SAME_AS_OLD);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("並發") || ex.Message.Contains("衝突"))
        {
            _logger.LogWarning(ex, "變更密碼失敗: {Message}", ex.Message);
            return Conflict(ex.Message, ResponseCodes.CONCURRENT_UPDATE_CONFLICT);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "變更密碼時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 刪除帳號
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <param name="request">刪除帳號請求</param>
    /// <returns>無內容</returns>
    /// <response code="200">刪除成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">未授權</response>
    /// <response code="404">帳號不存在</response>
    /// <response code="422">無法刪除當前登入帳號或最後一個有效帳號</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponseModel), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 422)]
    public async Task<IActionResult> DeleteAccount(Guid id, [FromBody] DeleteAccountRequest request)
    {
        try
        {
            // 取得當前登入使用者 ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid operatorId))
            {
                return UnauthorizedResponse("無法識別當前登入使用者");
            }

            // 執行刪除
            await _accountService.DeleteAccountAsync(id, operatorId);

            return Success("帳號刪除成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "刪除帳號失敗: {Message}", ex.Message);
            return NotFound("帳號不存在", ResponseCodes.NOT_FOUND);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("當前登入"))
        {
            _logger.LogWarning(ex, "刪除帳號失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.CANNOT_DELETE_SELF);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("最後一個"))
        {
            _logger.LogWarning(ex, "刪除帳號失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.LAST_ACCOUNT_CANNOT_DELETE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除帳號時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }
}
