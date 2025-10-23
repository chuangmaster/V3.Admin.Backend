using V3.Admin.Backend.Interfaces;

namespace V3.Admin.Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> ValidateCredentialsAsync(string id, string password)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                return false;
            }

            // In a real application, you would use a secure password hashing and comparison algorithm.
            // For example, using BCrypt.Net:
            // return BCrypt.Net.BCrypt.Verify(password, user.Password);
            return user.Password == password;
        }
    }
}