using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using V3.Admin.Backend.Middleware;
using V3.Admin.Backend.Models;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Controllers;

/// <summary>
/// 客戶管理控制器
/// </summary>
/// <remarks>
/// 提供服務單建立流程所需的客戶搜尋與新增端點。
/// </remarks>
[Route("api/customers")]
[Authorize]
public class CustomerController : BaseApiController
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="customerService">客戶服務</param>
    /// <param name="logger">日誌記錄器</param>
    public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// 搜尋客戶（分頁）
    /// </summary>
    /// <remarks>
    /// 依姓名/電話/Email/身分證字號查詢客戶。
    /// 需要權限: customer.read
    /// </remarks>
    /// <param name="pageNumber">頁碼（從 1 開始）</param>
    /// <param name="pageSize">每頁筆數（1-100）</param>
    /// <param name="request">搜尋條件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分頁客戶清單</returns>
    [HttpGet("search")]
    [RequirePermission("customer.read")]
    [ProducesResponseType(typeof(PagedApiResponseModel<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SearchCustomerRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
            }

            request ??= new SearchCustomerRequest();

            var paged = await _customerService.SearchCustomersAsync(
                pageNumber,
                pageSize,
                request,
                userId.Value,
                cancellationToken
            );

            var items = paged.Items.Select(dto => new CustomerResponse
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    IdNumber = dto.IdNumber,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt,
                    Version = dto.Version,
                })
                .ToList();

            return PagedSuccess(items, paged.PageNumber, paged.PageSize, paged.TotalCount, "查詢成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜尋客戶失敗 | TraceId: {TraceId}", TraceId);
            return InternalError("搜尋客戶失敗");
        }
    }

    /// <summary>
    /// 新增客戶
    /// </summary>
    /// <remarks>
    /// 建立一筆新客戶資料。
    /// 需要權限: customer.create
    /// </remarks>
    /// <param name="request">新增客戶請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的客戶資料</returns>
    [HttpPost]
    [RequirePermission("customer.create")]
    [ProducesResponseType(typeof(ApiResponseModel<CustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseModel), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return UnauthorizedResponse();
            }

            var dto = await _customerService.CreateCustomerAsync(request, userId.Value, cancellationToken);

            var response = new CustomerResponse
            {
                Id = dto.Id,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                IdNumber = dto.IdNumber,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                Version = dto.Version,
            };

            return Created(response, "建立成功");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "新增客戶失敗: {Message} | TraceId: {TraceId}", ex.Message, TraceId);
            return BusinessError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增客戶時發生未預期的錯誤 | TraceId: {TraceId}", TraceId);
            return InternalError("新增客戶失敗");
        }
    }
}
