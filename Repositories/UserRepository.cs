using V3.Admin.Backend.Interfaces;

namespace V3.Admin.Backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly List<User> _users = new()
        {
            // In a real application, this data would come from a database.
            // Passwords should be hashed.
            new User { Id = "admin", Password = "password" }
        };

        public Task<User?> GetUserByIdAsync(string id)
        { 
            var user = _users.FirstOrDefault(u => u.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }
}