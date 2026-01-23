using FluentAssertions;
using FluentValidation.TestHelper;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;

namespace V3.Admin.Backend.Tests.Unit.Validators;

/// <summary>
/// 密碼重設請求驗證器測試
/// </summary>
public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator;

    public ResetPasswordRequestValidatorTests()
    {
        _validator = new ResetPasswordRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword456",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyNewPassword_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("新密碼為必填");
    }

    [Fact]
    public void Validate_NullNewPassword_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = null!,
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Theory]
    [InlineData("short")]           // 太短
    [InlineData("nouppercase123")]  // 無大寫
    [InlineData("NOLOWERCASE123")]  // 無小寫
    [InlineData("NoNumber")]        // 無數字
    public void Validate_WeakPassword_ShouldFail(string weakPassword)
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = weakPassword,
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_NewPasswordTooShort_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "Pass1", // 只有 5 個字元
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("密碼長度至少 8 個字元");
    }

    [Fact]
    public void Validate_NewPasswordTooLong_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = new string('A', 101) + "1a", // 超過 100 個字元
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("密碼長度最多 100 個字元");
    }

    [Fact]
    public void Validate_NewPasswordNoUpperCase_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "nouppercase123",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("密碼必須包含至少一個大寫字母");
    }

    [Fact]
    public void Validate_NewPasswordNoLowerCase_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NOLOWERCASE123",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("密碼必須包含至少一個小寫字母");
    }

    [Fact]
    public void Validate_NewPasswordNoNumber_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NoNumberPassword",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("密碼必須包含至少一個數字");
    }

    [Fact]
    public void Validate_NegativeVersion_ShouldFail()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword456",
            Version = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Version)
            .WithErrorMessage("版本號必須大於或等於 0");
    }

    [Fact]
    public void Validate_VersionZero_ShouldPass()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "NewPassword456",
            Version = 0
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_StrongPassword_ShouldPass()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            NewPassword = "StrongP@ssw0rd123!",
            Version = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
