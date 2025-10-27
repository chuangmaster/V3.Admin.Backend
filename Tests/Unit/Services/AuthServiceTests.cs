using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_mockUserRepository.Object, _mockJwtService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "admin",
            Password = "Admin@123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12),
            DisplayName = "管理員",
            Version = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockJwtService
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("mock_jwt_token");

        _mockJwtService
            .Setup(x => x.GetTokenExpirationTime())
            .Returns(DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("mock_jwt_token");
        result.User.Should().NotBeNull();
        result.User.Username.Should().Be("admin");
        result.User.DisplayName.Should().Be("管理員");

        _mockUserRepository.Verify(x => x.GetByUsernameAsync(loginDto.Username), Times.Once);
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "nonexistent",
            Password = "Password123"
        };

        _mockUserRepository
            .Setup(x => x.GetByUsernameAsync(loginDto.Username))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _authService.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("帳號或密碼錯誤");

        _mockUserRepository.Verify(x => x.GetByUsernameAsync(loginDto.Username), Times.Once);
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_DeletedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "deleted_user",
            Password = "Password123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "deleted_user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123", 12),
            DisplayName = "已刪除使用者",
            Version = 1,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _authService.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("帳號或密碼錯誤");

        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "admin",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword", 12),
            DisplayName = "管理員",
            Version = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(x => x.GetByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _authService.LoginAsync(loginDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("帳號或密碼錯誤");

        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
