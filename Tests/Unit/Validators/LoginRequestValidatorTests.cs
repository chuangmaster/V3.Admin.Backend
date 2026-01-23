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
            Account = "admin",
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
    public void Validate_EmptyAccount_ShouldFail(string? account)
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = account!,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Account" && e.ErrorMessage.Contains("不可為空"));
    }

    [Theory]
    [InlineData("ab")]  // 太短
    [InlineData("a")]   // 太短
    public void Validate_AccountTooShort_ShouldFail(string account)
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = account,
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Account" && e.ErrorMessage.Contains("長度"));
    }

    [Fact]
    public void Validate_AccountTooLong_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Account = new string('a', 21), // 超過 20 字元
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Account" && e.ErrorMessage.Contains("長度"));
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
            Account = "admin",
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
            Account = "admin",
            Password = "Pass123" // 只有 7 字元
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("至少 8 個字元"));
    }
}
