using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// 客戶業務邏輯介面
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// 依條件搜尋客戶 (分頁)
    /// </summary>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁筆數 (1-100)</param>
    /// <param name="request">搜尋條件</param>
    /// <param name="operatorId">操作者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分頁搜尋結果</returns>
    Task<PagedResultDto<CustomerDto>> SearchCustomersAsync(
        int pageNumber,
        int pageSize,
        SearchCustomerRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 建立客戶
    /// </summary>
    /// <param name="request">新增客戶資料</param>
    /// <param name="createdBy">建立者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立後的客戶資料</returns>
    Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 依身分證字號/外籍人士格式查詢客戶
    /// </summary>
    /// <param name="idNumber">身分證字號/外籍人士格式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>客戶資料，若不存在則回傳 null</returns>
    Task<CustomerDto?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default);
}
