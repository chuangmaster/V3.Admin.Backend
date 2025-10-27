using V3.Admin.Backend.Models.Entities;

namespace V3.Admin.Backend.Repositories.Interfaces;

/// <summary>
/// 使用者資料存取介面
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsAsync(string username);
    Task<IEnumerable<User>> GetAllAsync(int pageNumber, int pageSize);
    Task<bool> CreateAsync(User user);
    Task<bool> UpdateAsync(User user, int expectedVersion);
    Task<bool> DeleteAsync(Guid id, Guid operatorId);
    Task<int> CountActiveAsync();
}
