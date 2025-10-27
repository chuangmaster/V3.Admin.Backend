using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Models;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// API 控制器基類，提供統一的響應格式和 HTTP 狀態碼處理
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// 取得當前請求的追蹤 ID
    /// </summary>
    protected string TraceId => HttpContext.TraceIdentifier;

    /// <summary>
    /// 回傳成功響應 (200 OK)
    /// </summary>
    protected IActionResult Success<T>(T? data = default, string message = "操作成功")
    {
        var response = ApiResponseModel<T>.CreateSuccess(data, message, ResponseCodes.SUCCESS);
        response.TraceId = TraceId;
        return Ok(response);
    }

    /// <summary>
    /// 回傳成功響應 (200 OK) - 無資料
    /// </summary>
    protected IActionResult Success(string message = "操作成功")
    {
        var response = ApiResponseModel.CreateSuccess(message, ResponseCodes.SUCCESS);
        response.TraceId = TraceId;
        return Ok(response);
    }

    /// <summary>
    /// 回傳建立成功響應 (201 Created)
    /// </summary>
    protected IActionResult Created<T>(T data, string message = "建立成功")
    {
        var response = ApiResponseModel<T>.CreateSuccess(data, message, ResponseCodes.CREATED);
        response.TraceId = TraceId;
        return StatusCode(201, response);
    }

    /// <summary>
    /// 回傳驗證錯誤響應 (400 Bad Request)
    /// </summary>
    protected IActionResult ValidationError(string message)
    {
        var response = ApiResponseModel.CreateFailure(message, ResponseCodes.VALIDATION_ERROR);
        response.TraceId = TraceId;
        return BadRequest(response);
    }

    /// <summary>
    /// 回傳未授權響應 (401 Unauthorized)
    /// </summary>
    protected IActionResult UnauthorizedResponse(string message = "未授權,請先登入")
    {
        var response = ApiResponseModel.CreateFailure(message, ResponseCodes.UNAUTHORIZED);
        response.TraceId = TraceId;
        return StatusCode(401, response);
    }

    /// <summary>
    /// 回傳禁止存取響應 (403 Forbidden)
    /// </summary>
    protected IActionResult Forbidden(string message = "您沒有權限執行此操作")
    {
        var response = ApiResponseModel.CreateFailure(message, ResponseCodes.FORBIDDEN);
        response.TraceId = TraceId;
        return StatusCode(403, response);
    }

    /// <summary>
    /// 回傳資源未找到響應 (404 Not Found)
    /// </summary>
    protected IActionResult NotFound(
        string message = "資源未找到",
        string code = ResponseCodes.NOT_FOUND
    )
    {
        var response = ApiResponseModel.CreateFailure(message, code);
        response.TraceId = TraceId;
        return StatusCode(404, response);
    }

    /// <summary>
    /// 回傳衝突錯誤響應 (409 Conflict)
    /// </summary>
    protected IActionResult Conflict(
        string message = "資源衝突",
        string code = ResponseCodes.CONCURRENT_UPDATE_CONFLICT
    )
    {
        var response = ApiResponseModel.CreateFailure(message, code);
        response.TraceId = TraceId;
        return StatusCode(409, response);
    }

    /// <summary>
    /// 回傳業務邏輯錯誤響應 (422 Unprocessable Entity)
    /// </summary>
    protected IActionResult BusinessError(
        string message,
        string code = ResponseCodes.VALIDATION_ERROR
    )
    {
        var response = ApiResponseModel.CreateFailure(message, code);
        response.TraceId = TraceId;
        return StatusCode(422, response);
    }

    /// <summary>
    /// 回傳內部伺服器錯誤響應 (500 Internal Server Error)
    /// </summary>
    protected IActionResult InternalError(
        string message = "內部伺服器錯誤",
        string code = ResponseCodes.INTERNAL_ERROR
    )
    {
        var response = ApiResponseModel.CreateFailure(message, code);
        response.TraceId = TraceId;
        return StatusCode(500, response);
    }
}
