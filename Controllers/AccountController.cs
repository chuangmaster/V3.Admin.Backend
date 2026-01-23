using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
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
        ILogger<AccountController> logger
    )
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢當前登入用戶的個人資料
    /// </summary>
    /// <remarks>
    /// 允許已登入用戶查詢自己的個人資料，包含用戶名稱、顯示名稱和角色清單
    ///
    /// 需要的權限: user.profile.read
    /// </remarks>
    /// <returns>當前用戶的個人資料</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="401">未授權 - Token 無效、過期或用戶已停用</response>
    /// <response code="403">禁止存取 - 無 user.profile.read 權限</response>
    /// <response code="404">用戶不存在</response>
    [HttpGet("me")]
    [RequirePermission("user.profile.read")]
    [ProducesResponseType(typeof(ApiResponseModel<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            // 從 JWT Token 中取得當前用戶 ID
            var userId = GetUserId();
            if (userId is null)
            {
                _logger.LogWarning("查詢個人資料失敗: 無法識別當前登入使用者");
                return UnauthorizedResponse("未授權，請先登入");
            }

            // 查詢用戶個人資料 (Service 層回傳 DTO)
            var profileDto = await _accountService.GetUserProfileAsync(userId.Value);
            if (profileDto is null)
            {
                _logger.LogWarning("查詢個人資料失敗: 用戶 {UserId} 不存在或已刪除", userId);
                return NotFound("用戶不存在", ResponseCodes.NOT_FOUND);
            }

            // 轉換 DTO 為 Response 物件回傳給客戶端
            var response = new UserProfileResponse
            {
                Id = profileDto.Id,
                Account = profileDto.Account,
                DisplayName = profileDto.DisplayName,
                Roles = profileDto.Roles,
                Permissions = profileDto.Permissions ?? new List<string>(),
                Version = profileDto.Version,
            };

            _logger.LogInformation("成功查詢用戶 {UserId} 的個人資料", userId);
            return Success(response, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢個人資料時發生未預期的錯誤");
            return InternalError("系統錯誤，請稍後再試");
        }
    }

    /// <summary>
    /// 查詢帳號列表
    /// </summary>
    /// <param name="pageNumber">頁碼 (預設 1)</param>
    /// <param name="pageSize">每頁數量 (預設 10)</param>
    /// <param name="searchKeyword">搜尋關鍵字 (比對 account 和 display_name，不區分大小寫，選填)</param>
    /// <returns>帳號列表</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="401">未授權</response>
    /// <response code="403">禁止存取 - 無 account.read 權限</response>
    [HttpGet]
    [RequirePermission("account.read")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountListResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchKeyword = null
    )
    {
        try
        {
            AccountListDto result = await _accountService.GetAccountsAsync(
                pageNumber,
                pageSize,
                searchKeyword
            );

            AccountListResponse response = new AccountListResponse
            {
                Items = result
                    .Items.Select(dto => new AccountResponse
                    {
                        Id = dto.Id,
                        Account = dto.Account,
                        DisplayName = dto.DisplayName,
                        CreatedAt = dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                        Version = dto.Version,
                    })
                    .ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize),
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
    /// <response code="403">禁止存取 - 無 account.read 權限</response>
    /// <response code="404">帳號不存在</response>
    [HttpGet("{id}")]
    [RequirePermission("account.read")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        try
        {
            AccountDto result = await _accountService.GetAccountByIdAsync(id);

            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Account = result.Account,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
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
    /// <response code="403">禁止存取 - 無 account.create 權限</response>
    /// <response code="422">帳號已存在</response>
    [HttpPost]
    [RequirePermission("account.create")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 422)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            // 轉換為 DTO
            CreateAccountDto createDto = new CreateAccountDto
            {
                Account = request.Account,
                Password = request.Password,
                DisplayName = request.DisplayName,
            };

            // 執行新增
            AccountDto result = await _accountService.CreateAccountAsync(createDto);

            // 轉換為 Response
            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Account = result.Account,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
            };

            return Created(response, "帳號建立成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "新增帳號失敗: {Message}", ex.Message);
            return BusinessError(ex.Message, ResponseCodes.ACCOUNT_EXISTS);
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
    /// <returns>更新後的帳號資訊</returns>
    /// <response code="200">更新成功</response>
    /// <response code="400">輸入驗證錯誤</response>
    /// <response code="401">未授權</response>
    /// <response code="403">禁止存取 - 無 account.update 權限</response>
    /// <response code="404">帳號不存在</response>
    /// <response code="409">並發更新衝突</response>
    [HttpPut("{id}")]
    [RequirePermission("account.update")]
    [ProducesResponseType(typeof(ApiResponseModel<AccountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 409)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            // 轉換為 DTO
            UpdateAccountDto updateDto = new UpdateAccountDto
            {
                Id = id,
                DisplayName = request.DisplayName,
                Version = request.Version,
            };

            // 執行更新
            AccountDto result = await _accountService.UpdateAccountAsync(updateDto);

            // 轉換為 Response
            AccountResponse response = new AccountResponse
            {
                Id = result.Id,
                Account = result.Account,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
            };

            return Success(response, "更新成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "更新帳號失敗: {Message}", ex.Message);
            return NotFound("帳號不存在", ResponseCodes.NOT_FOUND);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("並發") || ex.Message.Contains("衝突"))
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
    /// 用戶修改自己的密碼
    /// </summary>
    /// <remarks>
    /// 允許已登入的用戶修改自己的密碼。
    ///
    /// **要求**:
    /// - 必須提供正確的舊密碼
    /// - 必須提供符合強度要求的新密碼
    /// - 必須提供當前的 version 號
    ///
    /// **行為**:
    /// - 密碼修改成功後,保持當前會話有效
    /// - 該用戶在其他設備的所有會話將失效
    /// - version 欄位遞增 1
    ///
    /// 需要的權限: user.profile.update
    /// </remarks>
    /// <param name="request">密碼修改請求</param>
    /// <returns>操作結果</returns>
    /// <response code="200">密碼修改成功</response>
    /// <response code="400">請求驗證失敗(舊密碼錯誤、新密碼強度不足或新密碼與舊密碼相同)</response>
    /// <response code="401">未授權(未登入或 token 無效)</response>
    /// <response code="403">權限不足(缺少 user.profile.update 權限)</response>
    /// <response code="409">併發衝突(version 不匹配)</response>
    [HttpPut("me/password")]
    [RequirePermission("user.profile.update")]
    [ProducesResponseType(typeof(ApiResponseModel), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 409)]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // 從 JWT Token 中取得當前用戶 ID
            var userId = GetUserId();
            if (userId is null)
            {
                _logger.LogWarning("修改密碼失敗: 無法識別當前登入使用者");
                return UnauthorizedResponse("未授權,請重新登入");
            }

            // 轉換為 DTO
            ChangePasswordDto changeDto = new ChangePasswordDto
            {
                Id = userId.Value,
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword,
                Version = request.Version,
            };

            // 執行變更密碼
            await _accountService.ChangePasswordAsync(changeDto);

            _logger.LogInformation("用戶 {UserId} 成功修改密碼", userId);
            return Success("密碼修改成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "修改密碼失敗: {Message}", ex.Message);
            return NotFound("用戶不存在", ResponseCodes.NOT_FOUND);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "修改密碼失敗: {Message}", ex.Message);
            return ValidationError("舊密碼錯誤");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("相同"))
        {
            _logger.LogWarning(ex, "修改密碼失敗: {Message}", ex.Message);
            return ValidationError("新密碼不可與舊密碼相同");
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("並發")
                || ex.Message.Contains("衝突")
                || ex.Message.Contains("更新")
            )
        {
            _logger.LogWarning(ex, "修改密碼失敗: {Message}", ex.Message);
            return Conflict(
                "密碼修改失敗,資料已被其他操作更新,請重新獲取最新資料後再試",
                ResponseCodes.CONCURRENT_UPDATE_CONFLICT
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密碼時發生未預期的錯誤");
            return InternalError("系統錯誤,請稍後再試");
        }
    }

    /// <summary>
    /// 管理員重設用戶密碼
    /// </summary>
    /// <remarks>
    /// 允許擁有 account.update 權限的管理員重設指定用戶的密碼。
    ///
    /// **要求**:
    /// - 操作者必須擁有 account.update 權限
    /// - 必須提供符合強度要求的新密碼
    /// - 必須提供目標用戶的當前 version 號
    ///
    /// **行為**:
    /// - 無需提供舊密碼
    /// - 密碼重設成功後,目標用戶的所有會話失效
    /// - version 欄位遞增 1
    /// - 操作記錄到 audit_logs 表
    /// - 不發送通知給目標用戶
    ///
    /// 需要的權限: account.update
    /// </remarks>
    /// <param name="id">目標用戶的 ID</param>
    /// <param name="request">密碼重設請求</param>
    /// <returns>操作結果</returns>
    /// <response code="200">密碼重設成功</response>
    /// <response code="400">請求驗證失敗(新密碼強度不足)</response>
    /// <response code="401">未授權(未登入或 token 無效)</response>
    /// <response code="403">權限不足(缺少 account.update 權限)</response>
    /// <response code="404">目標用戶不存在</response>
    /// <response code="409">併發衝突(version 不匹配)</response>
    [HttpPut("{id}/reset-password")]
    [RequirePermission("account.update")]
    [ProducesResponseType(typeof(ApiResponseModel), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 409)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            // 從 JWT Token 中取得當前操作者 ID
            var operatorId = GetUserId();
            if (operatorId is null)
            {
                _logger.LogWarning("重設密碼失敗: 無法識別當前登入使用者");
                return UnauthorizedResponse("未授權,請重新登入");
            }

            // 建立 DTO 並呼叫 Service 層
            var resetDto = new ResetPasswordDto
            {
                TargetUserId = id,
                NewPassword = request.NewPassword,
                Version = request.Version,
                OperatorId = operatorId.Value
            };

            await _accountService.ResetPasswordAsync(resetDto);

            _logger.LogInformation(
                "操作者 {OperatorId} 成功重設用戶 {TargetUserId} 的密碼",
                operatorId,
                id
            );
            return Success("密碼重設成功");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "重設密碼失敗: {Message}", ex.Message);
            return NotFound($"找不到 ID 為 {id} 的用戶", ResponseCodes.NOT_FOUND);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("並發")
                || ex.Message.Contains("衝突")
                || ex.Message.Contains("更新")
            )
        {
            _logger.LogWarning(ex, "重設密碼失敗: {Message}", ex.Message);
            return Conflict(
                "密碼重設失敗,資料已被其他操作更新,請重新獲取最新資料後再試",
                ResponseCodes.CONCURRENT_UPDATE_CONFLICT
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重設密碼時發生未預期的錯誤");
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
    /// <response code="403">禁止存取 - 無 account.delete 權限</response>
    /// <response code="404">帳號不存在</response>
    /// <response code="422">無法刪除當前登入帳號或最後一個有效帳號</response>
    [HttpDelete("{id}")]
    [RequirePermission("account.delete")]
    [ProducesResponseType(typeof(ApiResponseModel), 200)]
    [ProducesResponseType(typeof(ApiResponseModel), 400)]
    [ProducesResponseType(typeof(ApiResponseModel), 401)]
    [ProducesResponseType(typeof(ApiResponseModel), 403)]
    [ProducesResponseType(typeof(ApiResponseModel), 404)]
    [ProducesResponseType(typeof(ApiResponseModel), 422)]
    public async Task<IActionResult> DeleteAccount(Guid id, [FromBody] DeleteAccountRequest request)
    {
        try
        {
            // 取得當前登入使用者 ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (
                string.IsNullOrEmpty(userIdClaim)
                || !Guid.TryParse(userIdClaim, out Guid operatorId)
            )
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
