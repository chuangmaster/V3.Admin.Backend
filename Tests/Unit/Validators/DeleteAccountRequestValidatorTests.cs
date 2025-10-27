using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

public class DeleteAccountRequestValidatorTests
{
    private readonly DeleteAccountRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidConfirmation_ShouldPass()
    {
        // Arrange
        var request = new DeleteAccountRequest
        {
            Confirmation = "CONFIRM"
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
    public void Validate_EmptyConfirmation_ShouldFail(string? confirmation)
    {
        // Arrange
        var request = new DeleteAccountRequest
        {
            Confirmation = confirmation!
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Confirmation" && e.ErrorMessage.Contains("不可為空"));
    }

    [Theory]
    [InlineData("confirm")]     // 小寫
    [InlineData("Confirm")]     // 首字母大寫
    [InlineData("CONFIRMED")]   // 錯誤的文字
    [InlineData("YES")]         // 其他文字
    public void Validate_InvalidConfirmation_ShouldFail(string confirmation)
    {
        // Arrange
        var request = new DeleteAccountRequest
        {
            Confirmation = confirmation
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Confirmation" && e.ErrorMessage.Contains("必須輸入 'CONFIRM'"));
    }

    [Fact]
    public void Validate_ConfirmationWithWhitespace_ShouldFail()
    {
        // Arrange
        var request = new DeleteAccountRequest
        {
            Confirmation = " CONFIRM "
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Confirmation");
    }
}
