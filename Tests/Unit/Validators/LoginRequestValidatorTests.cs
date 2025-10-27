using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyUsername_ShouldFail(string? username)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = username!,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("不可為空"));
    }

    [Theory]
    [InlineData("ab")]  // 太短
    [InlineData("a")]   // 太短
    public void Validate_UsernameTooShort_ShouldFail(string username)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = username,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("長度"));
    }

    [Fact]
    public void Validate_UsernameTooLong_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = new string('a', 21), // 超過 20 字元
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("長度"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyPassword_ShouldFail(string? password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = password!
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("不可為空"));
    }

    [Fact]
    public void Validate_PasswordTooShort_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Pass123" // 只有 7 字元
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("至少 8 個字元"));
    }
}
