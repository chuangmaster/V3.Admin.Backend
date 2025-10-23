namespace V3.Admin.Backend.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(string id, string password);
}
