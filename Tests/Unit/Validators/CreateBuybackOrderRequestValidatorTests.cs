using FluentAssertions;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

/// <summary>
/// 建立收購單請求驗證器測試
/// </summary>
public class CreateBuybackOrderRequestValidatorTests
{
    private readonly CreateBuybackOrderRequestValidator _validator = new();

    /// <summary>
    /// 驗證有效請求應通過
    /// </summary>
    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            NewCustomer = new CreateCustomerRequest
            {
                Name = "王小明",
                PhoneNumber = "0912345678",
                Email = "test@example.com",
                IdNumber = "A123456789",
            },
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new()
                {
                    SequenceNumber = 1,
                    BrandName = "CHANEL",
                    StyleName = "Classic",
                    InternalCode = "INT-001",
                },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id-card.png",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// 同時提供 CustomerId 與 NewCustomer 應失敗
    /// </summary>
    [Fact]
    public void Validate_BothCustomerIdAndNewCustomer_ShouldFail()
    {
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            CustomerId = Guid.NewGuid(),
            NewCustomer = new CreateCustomerRequest
            {
                Name = "王小明",
                PhoneNumber = "0912345678",
                IdNumber = "A123456789",
            },
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new()
                {
                    SequenceNumber = 1,
                    BrandName = "CHANEL",
                    StyleName = "Classic",
                },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = Convert.ToBase64String(new byte[] { 1 }),
            IdCardImageContentType = "image/jpeg",
            IdCardImageFileName = "id-card.jpg",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("二擇一"));
    }

    /// <summary>
    /// 商品數量超過 4 件應失敗
    /// </summary>
    [Fact]
    public void Validate_ProductItemsMoreThanFour_ShouldFail()
    {
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            CustomerId = Guid.NewGuid(),
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new() { SequenceNumber = 1, BrandName = "B1", StyleName = "S1" },
                new() { SequenceNumber = 2, BrandName = "B2", StyleName = "S2" },
                new() { SequenceNumber = 3, BrandName = "B3", StyleName = "S3" },
                new() { SequenceNumber = 4, BrandName = "B4", StyleName = "S4" },
                new() { SequenceNumber = 1, BrandName = "B5", StyleName = "S5" },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = Convert.ToBase64String(new byte[] { 1 }),
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id-card.png",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("1-4"));
    }

    /// <summary>
    /// 身分證 Base64 不可解析應失敗
    /// </summary>
    [Fact]
    public void Validate_InvalidBase64_ShouldFail()
    {
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            CustomerId = Guid.NewGuid(),
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new()
                {
                    SequenceNumber = 1,
                    BrandName = "CHANEL",
                    StyleName = "Classic",
                },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = "not-base64",
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id-card.png",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Base64"));
    }
}
