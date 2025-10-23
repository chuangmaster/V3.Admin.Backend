namespace V3.Admin.Backend.Models;

public class User
{
    public required string Id { get; set; }
    public required string Password { get; set; } // In a real application, this should be a hashed password.
}