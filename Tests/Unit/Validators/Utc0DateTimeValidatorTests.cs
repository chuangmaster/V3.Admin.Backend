using FluentValidation;
using FluentValidation.TestHelper;
using V3.Admin.Backend.Validators;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Validators;

/// <summary>
/// Utc0DateTimeValidator 的單元測試
/// 測試各種時間格式驗證規則
/// </summary>
public class Utc0DateTimeValidatorTests
{
    #region MustBeUtc0 - DateTimeOffset 測試

    [Fact]
    public void MustBeUtc0_WithUtc0DateTime_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidator();
        var model = new TestModel
        {
            CreatedAt = new DateTimeOffset(2026, 1, 24, 6, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatedAt);
    }

    [Fact]
    public void MustBeUtc0_WithNonUtc0DateTime_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidator();
        var model = new TestModel
        {
            CreatedAt = new DateTimeOffset(2026, 1, 24, 14, 0, 0, TimeSpan.FromHours(8))
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedAt);
    }

    [Fact]
    public void MustBeUtc0_WithNegativeOffset_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidator();
        var model = new TestModel
        {
            CreatedAt = new DateTimeOffset(2026, 1, 24, 10, 0, 0, TimeSpan.FromHours(-5))
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedAt);
    }

    #endregion

    #region MustBeUtc0 - Nullable DateTimeOffset 測試

    [Fact]
    public void MustBeUtc0_WithNullValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithNullable();
        var model = new TestModelWithNullable
        {
            UpdatedAt = null
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UpdatedAt);
    }

    [Fact]
    public void MustBeUtc0_WithNullableUtc0DateTime_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithNullable();
        var model = new TestModelWithNullable
        {
            UpdatedAt = new DateTimeOffset(2026, 1, 24, 6, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UpdatedAt);
    }

    [Fact]
    public void MustBeUtc0_WithNullableNonUtc0DateTime_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithNullable();
        var model = new TestModelWithNullable
        {
            UpdatedAt = new DateTimeOffset(2026, 1, 24, 14, 0, 0, TimeSpan.FromHours(8))
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UpdatedAt);
    }

    #endregion

    #region MustBeUtc0IsoFormat - 字串格式測試

    [Theory]
    [InlineData("2026-01-24T06:00:00.123Z")]
    [InlineData("2026-01-24T06:00:00Z")]
    [InlineData("2026-12-31T23:59:59.999Z")]
    [InlineData("1900-01-01T00:00:00.000Z")]
    public void MustBeUtc0IsoFormat_WithValidFormat_ShouldNotHaveValidationError(string dateString)
    {
        // Arrange
        var validator = new TestValidatorWithString();
        var model = new TestModelWithString
        {
            DateString = dateString
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateString);
    }

    [Theory]
    [InlineData("2026-01-24T14:00:00+08:00")] // 帶時區偏移
    [InlineData("2026-01-24 06:00:00")] // 缺少 T
    [InlineData("2026/01/24T06:00:00Z")] // 錯誤的分隔符
    [InlineData("2026-01-24T06:00:00")] // 缺少 Z
    [InlineData("2026-01-24")] // 只有日期
    [InlineData("not-a-date")] // 無效字串
    [InlineData("")] // 空字串
    [InlineData("   ")] // 空白字串
    public void MustBeUtc0IsoFormat_WithInvalidFormat_ShouldHaveValidationError(string dateString)
    {
        // Arrange
        var validator = new TestValidatorWithString();
        var model = new TestModelWithString
        {
            DateString = dateString
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateString);
    }

    #endregion

    #region MustBeInReasonableRange 測試

    [Fact]
    public void MustBeInReasonableRange_WithDateInRange_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithRange();
        var model = new TestModelWithRange
        {
            BirthDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void MustBeInReasonableRange_WithDateBeforeMin_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithRange();
        var model = new TestModelWithRange
        {
            BirthDate = new DateTimeOffset(1800, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void MustBeInReasonableRange_WithDateAfterMax_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithRange();
        var model = new TestModelWithRange
        {
            BirthDate = new DateTimeOffset(2200, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    #endregion

    #region MustNotBeFutureDate 測試

    [Fact]
    public void MustNotBeFutureDate_WithPastDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithFutureCheck();
        var model = new TestModelWithFutureCheck
        {
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatedAt);
    }

    [Fact]
    public void MustNotBeFutureDate_WithCurrentDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithFutureCheck();
        var model = new TestModelWithFutureCheck
        {
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatedAt);
    }

    [Fact]
    public void MustNotBeFutureDate_WithFutureDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithFutureCheck();
        var model = new TestModelWithFutureCheck
        {
            CreatedAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedAt);
    }

    #endregion

    #region MustBeFutureDate 測試

    [Fact]
    public void MustBeFutureDate_WithFutureDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithMustBeFuture();
        var model = new TestModelWithMustBeFuture
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledAt);
    }

    [Fact]
    public void MustBeFutureDate_WithPastDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new TestValidatorWithMustBeFuture();
        var model = new TestModelWithMustBeFuture
        {
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Act
        var result = validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ScheduledAt);
    }

    #endregion

    #region 測試輔助類別

    private class TestModel
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    private class TestValidator : AbstractValidator<TestModel>
    {
        public TestValidator()
        {
            RuleFor(x => x.CreatedAt).MustBeUtc0();
        }
    }

    private class TestModelWithNullable
    {
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    private class TestValidatorWithNullable : AbstractValidator<TestModelWithNullable>
    {
        public TestValidatorWithNullable()
        {
            RuleFor(x => x.UpdatedAt).MustBeUtc0();
        }
    }

    private class TestModelWithString
    {
        public string DateString { get; set; } = string.Empty;
    }

    private class TestValidatorWithString : AbstractValidator<TestModelWithString>
    {
        public TestValidatorWithString()
        {
            RuleFor(x => x.DateString).MustBeUtc0IsoFormat();
        }
    }

    private class TestModelWithRange
    {
        public DateTimeOffset BirthDate { get; set; }
    }

    private class TestValidatorWithRange : AbstractValidator<TestModelWithRange>
    {
        public TestValidatorWithRange()
        {
            RuleFor(x => x.BirthDate).MustBeInReasonableRange();
        }
    }

    private class TestModelWithFutureCheck
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    private class TestValidatorWithFutureCheck : AbstractValidator<TestModelWithFutureCheck>
    {
        public TestValidatorWithFutureCheck()
        {
            RuleFor(x => x.CreatedAt).MustNotBeFutureDate();
        }
    }

    private class TestModelWithMustBeFuture
    {
        public DateTimeOffset ScheduledAt { get; set; }
    }

    private class TestValidatorWithMustBeFuture : AbstractValidator<TestModelWithMustBeFuture>
    {
        public TestValidatorWithMustBeFuture()
        {
            RuleFor(x => x.ScheduledAt).MustBeFutureDate();
        }
    }

    #endregion
}
