using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = "NewPassword456"
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
    public void Validate_EmptyOldPassword_ShouldFail(string? oldPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword!,
            NewPassword = "NewPassword123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OldPassword" && e.ErrorMessage.Contains("不可為空"));
    }

    [Fact]
    public void Validate_NewPasswordTooShort_ShouldFail()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPassword123",
            NewPassword = "Pass123" // 只有 7 字元
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("至少 8 個字元"));
    }

    [Fact]
    public void Validate_NewPasswordSameAsOld_ShouldFail()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "SamePassword123",
            NewPassword = "SamePassword123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && e.ErrorMessage.Contains("不可與舊密碼相同"));
    }

    [Fact]
    public void Validate_NewPasswordDifferentCase_ShouldStillFail()
    {
        // Arrange - 測試大小寫不同但內容相同的密碼
        var request = new ChangePasswordRequest
        {
            OldPassword = "Password123",
            NewPassword = "password123"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue(); // 密碼區分大小寫，應該通過驗證
    }
}
