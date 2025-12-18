using System.Text.Json;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 客戶服務實作
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CustomerService> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="customerRepository">客戶資料存取層</param>
    /// <param name="auditLogService">稽核日誌服務</param>
    /// <param name="logger">日誌記錄器</param>
    public CustomerService(
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService,
        ILogger<CustomerService> logger
    )
    {
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 依條件搜尋客戶 (分頁)
    /// </summary>
    public async Task<PagedResultDto<CustomerDto>> SearchCustomersAsync(
        int pageNumber,
        int pageSize,
        SearchCustomerRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var (items, totalCount) = await _customerRepository.SearchAsync(
            pageNumber,
            pageSize,
            request.Name,
            request.PhoneNumber,
            request.Email,
            request.IdNumber,
            cancellationToken
        );

        var result = new PagedResultDto<CustomerDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    request.Name,
                    request.PhoneNumber,
                    request.Email,
                    request.IdNumber,
                    result.TotalCount,
                    result.PageNumber,
                    result.PageSize,
                }
            );

            await _auditLogService.LogOperationAsync(
                operatorId,
                "system",
                "query",
                "customer",
                targetId: null,
                beforeState: null,
                afterState: afterState,
                traceId: null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄客戶搜尋稽核日誌失敗");
        }

        return result;
    }

    /// <summary>
    /// 建立客戶
    /// </summary>
    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        Customer? existed = await _customerRepository.GetByIdNumberAsync(
            request.IdNumber,
            cancellationToken
        );

        if (existed is not null)
        {
            throw new InvalidOperationException("身分證字號已存在,不可重複建立");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IdNumber = request.IdNumber,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false,
            Version = 1,
        };

        Customer created = await _customerRepository.CreateAsync(
            customer,
            transaction: null,
            cancellationToken: cancellationToken
        );

        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    created.Id,
                    created.Name,
                    created.PhoneNumber,
                    created.Email,
                    created.IdNumber,
                    created.CreatedAt,
                }
            );

            await _auditLogService.LogOperationAsync(
                createdBy,
                "system",
                "create",
                "customer",
                created.Id,
                beforeState: null,
                afterState: afterState,
                traceId: null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄新增客戶稽核日誌失敗: CustomerId={CustomerId}", created.Id);
        }

        return MapToDto(created);
    }

    /// <summary>
    /// 依身分證字號/外籍人士格式查詢客戶
    /// </summary>
    public async Task<CustomerDto?> GetByIdNumberAsync(
        string idNumber,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            throw new ArgumentException("idNumber 不可為空", nameof(idNumber));
        }

        Customer? customer = await _customerRepository.GetByIdNumberAsync(idNumber, cancellationToken);
        return customer is null ? null : MapToDto(customer);
    }

    /// <summary>
    /// 將 Customer 實體映射為 CustomerDto
    /// </summary>
    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            IdNumber = customer.IdNumber,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Version = customer.Version,
        };
    }
}
