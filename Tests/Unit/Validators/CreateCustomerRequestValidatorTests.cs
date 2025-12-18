using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

/// <summary>
/// 新增客戶請求驗證器測試
/// </summary>
public class CreateCustomerRequestValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    /// <summary>
    /// 驗證有效請求應通過
    /// </summary>
    [Theory]
    [InlineData("A123456789")]
    [InlineData("a123456789")]
    [InlineData("12345678AB")]
    [InlineData("12345678ab")]
    public void Validate_ValidRequest_ShouldPass(string idNumber)
    {
        var request = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = "0912345678",
            Email = "test@example.com",
            IdNumber = idNumber,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// 電話格式錯誤應失敗
    /// </summary>
    [Theory]
    [InlineData("123")]
    [InlineData("0912-34567")]
    [InlineData("091234567")]
    public void Validate_InvalidPhoneNumber_ShouldFail(string phone)
    {
        var request = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = phone,
            IdNumber = "A123456789",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerRequest.PhoneNumber));
    }

    /// <summary>
    /// 身分證/居留證格式錯誤應失敗
    /// </summary>
    [Theory]
    [InlineData("A123")]
    [InlineData("1234567890")]
    [InlineData("AB12345678")]
    public void Validate_InvalidIdNumber_ShouldFail(string idNumber)
    {
        var request = new CreateCustomerRequest
        {
            Name = "王小明",
            PhoneNumber = "0912345678",
            IdNumber = idNumber,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerRequest.IdNumber));
    }
}
