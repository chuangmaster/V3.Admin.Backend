using BCrypt.Net;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Responses;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 帳號管理服務實作
/// </summary>
/// <remarks>
/// 負責處理帳號管理相關的業務邏輯
/// </remarks>
public class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="userRepository">使用者資料存取層</param>
    /// <param name="userRoleRepository">使用者角色資料存取層</param>
    /// <param name="rolePermissionRepository">角色權限資料存取層</param>
    /// <param name="logger">日誌記錄器</param>
    public AccountService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRolePermissionRepository rolePermissionRepository,
        ILogger<AccountService> logger
    )
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 新增帳號
    /// </summary>
    /// <param name="dto">新增帳號資訊</param>
    /// <returns>新建立的帳號資訊</returns>
    /// <exception cref="InvalidOperationException">帳號已存在</exception>
    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto dto)
    {
        // 檢查帳號是否已存在 (不區分大小寫)
        var usernameExists = await _userRepository.ExistsAsync(dto.Username.ToLowerInvariant());
        if (usernameExists)
        {
            _logger.LogWarning("新增帳號失敗: 帳號 {Username} 已存在", dto.Username);
            throw new InvalidOperationException($"帳號 {dto.Username} 已存在");
        }

        // 雜湊密碼
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

        // 建立 User Entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = dto.DisplayName,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Version = 1,
        };

        // 儲存至資料庫
        await _userRepository.CreateAsync(user);

        _logger.LogInformation("成功建立帳號 {Username} (ID: {UserId})", user.Username, user.Id);

        // 轉換為 DTO 回傳
        return new AccountDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Version = user.Version,
        };
    }

    /// <summary>
    /// 更新帳號資訊
    /// </summary>
    /// <param name="dto">更新帳號資訊</param>
    /// <returns>更新後的帳號資訊</returns>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="InvalidOperationException">並發更新衝突</exception>
    public async Task<AccountDto> UpdateAccountAsync(UpdateAccountDto dto)
    {
        // 查詢使用者
        User? user = await _userRepository.GetByIdAsync(dto.Id);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("更新帳號失敗: 帳號 {UserId} 不存在", dto.Id);
            throw new KeyNotFoundException($"帳號不存在");
        }

        // 檢查版本號 (樂觀並發控制)
        if (user.Version != dto.Version)
        {
            _logger.LogWarning(
                "更新帳號失敗: 帳號 {UserId} 版本衝突 (期望: {ExpectedVersion}, 實際: {ActualVersion})",
                dto.Id,
                dto.Version,
                user.Version
            );
            throw new InvalidOperationException("資料已被其他使用者更新,請重新載入後再試");
        }

        // 更新資料
        user.DisplayName = dto.DisplayName;
        user.UpdatedAt = DateTime.UtcNow;

        // 儲存至資料庫 (傳入期望的版本號)
        bool success = await _userRepository.UpdateAsync(user, dto.Version);
        if (!success)
        {
            _logger.LogWarning("更新帳號失敗: 帳號 {UserId} 更新失敗", dto.Id);
            throw new InvalidOperationException("資料已被其他使用者更新,請重新載入後再試");
        }

        _logger.LogInformation("成功更新帳號 {Username} (ID: {UserId})", user.Username, user.Id);

        // 轉換為 DTO 回傳 (版本號已在資料庫自動遞增)
        return new AccountDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Version = user.Version + 1, // Repository 會遞增版本號
        };
    }

    /// <summary>
    /// 變更密碼
    /// </summary>
    /// <param name="dto">變更密碼資訊</param>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="UnauthorizedAccessException">舊密碼錯誤</exception>
    /// <exception cref="InvalidOperationException">新密碼與舊密碼相同或並發更新衝突</exception>
    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        // 查詢使用者
        User? user = await _userRepository.GetByIdAsync(dto.Id);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("變更密碼失敗: 帳號 {UserId} 不存在", dto.Id);
            throw new KeyNotFoundException($"帳號不存在");
        }

        // 驗證舊密碼
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
        {
            _logger.LogWarning("變更密碼失敗: 帳號 {UserId} 的舊密碼錯誤", dto.Id);
            throw new UnauthorizedAccessException("舊密碼錯誤");
        }

        // 檢查新舊密碼是否相同
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("變更密碼失敗: 帳號 {UserId} 的新密碼與舊密碼相同", dto.Id);
            throw new InvalidOperationException("新密碼不可與舊密碼相同");
        }

        // 檢查版本號 (樂觀並發控制)
        if (user.Version != dto.Version)
        {
            _logger.LogWarning(
                "變更密碼失敗: 帳號 {UserId} 版本衝突 (期望: {ExpectedVersion}, 實際: {ActualVersion})",
                dto.Id,
                dto.Version,
                user.Version
            );
            throw new InvalidOperationException("資料已被其他使用者更新,請重新載入後再試");
        }

        // 雜湊新密碼
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        // 儲存至資料庫 (傳入期望的版本號)
        bool success = await _userRepository.UpdateAsync(user, dto.Version);
        if (!success)
        {
            _logger.LogWarning("變更密碼失敗: 帳號 {UserId} 更新失敗", dto.Id);
            throw new InvalidOperationException("資料已被其他使用者更新,請重新載入後再試");
        }

        _logger.LogInformation(
            "成功變更帳號 {Username} (ID: {UserId}) 的密碼",
            user.Username,
            user.Id
        );
    }

    /// <summary>
    /// 查詢單一帳號
    /// </summary>
    /// <param name="id">帳號 ID</param>
    /// <returns>帳號資訊</returns>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    public async Task<AccountDto> GetAccountByIdAsync(Guid id)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("查詢帳號失敗: 帳號 {UserId} 不存在", id);
            throw new KeyNotFoundException("帳號不存在");
        }

        return new AccountDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Version = user.Version,
        };
    }

    /// <summary>
    /// 查詢帳號列表 (分頁，支援 searchKeyword 搜尋)
    /// /// </summary>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁數量</param>
    /// <param name="searchKeyword">搜尋關鍵字 (比對 username 和 display_name，不區分大小寫)</param>
    /// <returns>帳號列表</returns>
    public async Task<AccountListDto> GetAccountsAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null
    )
    {
        // 確保頁碼與每頁數量合理
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        // 查詢帳號列表 (搜尋時傳遞 searchKeyword 參數)
        IEnumerable<User> users = await _userRepository.SearchAsync(
            pageNumber,
            pageSize,
            searchKeyword
        );
        int totalCount = await _userRepository.CountAsync(searchKeyword);

        // 轉換為 DTO
        List<AccountDto> items = users
            .Select(user => new AccountDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Version = user.Version,
            })
            .ToList();

        return new AccountListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    /// <summary>
    /// 刪除帳號 (軟刪除)
    /// </summary>
    /// <param name="id">要刪除的帳號 ID</param>
    /// <param name="operatorId">執行刪除操作的使用者 ID</param>
    /// <exception cref="KeyNotFoundException">帳號不存在</exception>
    /// <exception cref="InvalidOperationException">無法刪除當前登入帳號或最後一個有效帳號</exception>
    public async Task DeleteAccountAsync(Guid id, Guid operatorId)
    {
        // 查詢要刪除的帳號
        User? user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("刪除帳號失敗: 帳號 {UserId} 不存在", id);
            throw new KeyNotFoundException("帳號不存在");
        }

        // 不可刪除當前登入的帳號
        if (id == operatorId)
        {
            _logger.LogWarning("刪除帳號失敗: 使用者 {UserId} 嘗試刪除自己的帳號", id);
            throw new InvalidOperationException("無法刪除當前登入的帳號");
        }

        // 檢查是否為最後一個有效帳號
        int activeCount = await _userRepository.CountActiveAsync();
        if (activeCount <= 1)
        {
            _logger.LogWarning("刪除帳號失敗: 無法刪除最後一個有效帳號 (ID: {UserId})", id);
            throw new InvalidOperationException(
                "無法刪除最後一個有效帳號,系統至少需要保留一個帳號"
            );
        }

        // 執行軟刪除
        bool success = await _userRepository.DeleteAsync(id, operatorId);
        if (!success)
        {
            _logger.LogWarning("刪除帳號失敗: 帳號 {UserId} 刪除操作失敗", id);
            throw new InvalidOperationException("刪除帳號失敗,請稍後再試");
        }

        _logger.LogInformation(
            "成功刪除帳號 {Username} (ID: {UserId}),操作者: {OperatorId}",
            user.Username,
            user.Id,
            operatorId
        );
    }

    /// <summary>
    /// 查詢用戶的個人資料（包含角色資訊）
    /// </summary>
    /// <param name="userId">用戶 ID</param>
    /// <returns>用戶個人資料 DTO，若用戶不存在或已刪除則回傳 null</returns>
    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        try
        {
            // 查詢用戶
            var user = await _userRepository.GetByIdAsync(userId);

            // 檢查用戶是否存在且未刪除
            if (user is null || user.IsDeleted)
            {
                _logger.LogWarning("查詢個人資料失敗: 用戶 {UserId} 不存在或已刪除", userId);
                return null;
            }

            // 查詢用戶的所有角色名稱（保留原本的字串清單）
            var roleNames = await _userRoleRepository.GetRoleNamesByUserIdAsync(userId);

            // 查詢用戶的角色指派記錄以取得 roleId，並聚合每個角色的權限代碼
            var userRoles = await _userRoleRepository.GetUserRolesAsync(userId);

            var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ur in userRoles)
            {
                try
                {
                    var perms = await _rolePermissionRepository.GetRolePermissionsAsync(ur.RoleId);
                    if (perms is null)
                    {
                        continue;
                    }

                    foreach (var p in perms)
                    {
                        if (!string.IsNullOrWhiteSpace(p.PermissionCode))
                        {
                            permissionSet.Add(p.PermissionCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "查詢角色 {RoleId} 的權限發生錯誤，會略過該角色",
                        ur.RoleId
                    );
                }
            }

            // 在 Service 層組合並回傳 DTO 物件（Permissions 為去重後的權限代碼）
            var profileDto = new UserProfileDto
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                Roles = roleNames ?? [],
                Permissions = permissionSet.OrderBy(x => x).ToList(),
            };

            _logger.LogInformation(
                "成功查詢用戶個人資料: {Username} (ID: {UserId}), 角色數: {RoleCount}, 權限數: {PermissionCount}",
                user.Username,
                user.Id,
                roleNames?.Count ?? 0,
                permissionSet.Count
            );

            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢用戶個人資料發生例外: UserId={UserId}", userId);
            throw;
        }
    }
}
