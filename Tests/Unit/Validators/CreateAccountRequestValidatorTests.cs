using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

public class CreateAccountRequestValidatorTests
{
    private readonly CreateAccountRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Account = "new_user",
            Password = "Password123",
            DisplayName = "新使用者"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("user@name")]  // 包含特殊字元
    [InlineData("user name")]  // 包含空格
    [InlineData("user-name")]  // 包含連字號
    [InlineData("使用者")]     // 包含中文
    public void Validate_AccountWithInvalidCharacters_ShouldFail(string account)
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Account = account,
            Password = "Password123",
            DisplayName = "測試使用者"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Account" && e.ErrorMessage.Contains("僅允許英數字與底線"));
    }

    [Fact]
    public void Validate_ValidAccountWithUnderscore_ShouldPass()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Account = "user_name_123",
            Password = "Password123",
            DisplayName = "測試使用者"
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
    public void Validate_EmptyDisplayName_ShouldFail(string? displayName)
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Account = "testuser",
            Password = "Password123",
            DisplayName = displayName!
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName" && e.ErrorMessage.Contains("不可為空"));
    }

    [Fact]
    public void Validate_DisplayNameTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Account = "testuser",
            Password = "Password123",
            DisplayName = new string('名', 101) // 超過 100 字元
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName" && e.ErrorMessage.Contains("長度"));
    }

    [Fact]
    public void Validate_PasswordWithUnicodeCharacters_ShouldPass()
    {
        // Arrange - 測試密碼支援所有 Unicode 字元
        var request = new CreateAccountRequest
        {
            Account = "testuser",
            Password = "密碼測試123😀",  // 包含中文和 emoji
            DisplayName = "測試使用者"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
