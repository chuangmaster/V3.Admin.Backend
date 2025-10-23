using V3.Admin.Backend.Models;

namespace V3.Admin.Backend.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(string id);
}
