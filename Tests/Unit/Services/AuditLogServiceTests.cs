using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 稽核日誌服務單元測試
/// </summary>
public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _mockRepository;
    private readonly Mock<ILogger<AuditLogService>> _mockLogger;
    private readonly AuditLogService _service;

    public AuditLogServiceTests()
    {
        _mockRepository = new Mock<IAuditLogRepository>();
        _mockLogger = new Mock<ILogger<AuditLogService>>();
        _service = new AuditLogService(_mockRepository.Object, _mockLogger.Object);
    }

    /// <summary>
    /// 測試記錄操作稽核日誌
    /// </summary>
    [Fact]
    public async Task LogOperationAsync_WithValidData_ReturnsAuditLog()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var expectedAuditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            OperatorName = "TestUser",
            OperationType = "create",
            TargetType = "permission",
            TargetId = targetId,
            OperationTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.LogAsync(
            It.IsAny<AuditLog>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAuditLog);

        // Act
        var result = await _service.LogOperationAsync(
            operatorId,
            "TestUser",
            "create",
            "permission",
            targetId);

        // Assert
        result.Should().NotBeNull();
        result.OperatorId.Should().Be(operatorId);
        result.OperationType.Should().Be("create");
        _mockRepository.Verify(r => r.LogAsync(
            It.IsAny<AuditLog>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 測試查詢稽核日誌by ID
    /// </summary>
    [Fact]
    public async Task GetAuditLogByIdAsync_WithValidId_ReturnsAuditLogDto()
    {
        // Arrange
        var auditLogId = Guid.NewGuid();
        var auditLog = new AuditLog
        {
            Id = auditLogId,
            OperatorName = "TestUser",
            OperationType = "create",
            TargetType = "permission",
            OperationTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(auditLogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        var result = await _service.GetAuditLogByIdAsync(auditLogId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(auditLogId);
        result.OperatorName.Should().Be("TestUser");
    }

    /// <summary>
    /// 測試查詢稽核日誌列表
    /// </summary>
    [Fact]
    public async Task GetAuditLogsAsync_WithValidRequest_ReturnsAuditLogs()
    {
        // Arrange
        var request = new QueryAuditLogRequest
        {
            PageNumber = 1,
            PageSize = 20
        };

        var auditLogs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), OperatorName = "User1", OperationType = "create" },
            new AuditLog { Id = Guid.NewGuid(), OperatorName = "User2", OperationType = "update" }
        };

        _mockRepository.Setup(r => r.GetLogsAsync(
            null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((auditLogs, 2L));

        // Act
        var result = await _service.GetAuditLogsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
    }

    /// <summary>
    /// 測試查詢稽核日誌 - JSON 序列化
    /// </summary>
    [Fact]
    public async Task LogOperationAsync_WithJsonStates_SerializesCorrectly()
    {
        // Arrange
        var beforeState = JsonSerializer.Serialize(new { Name = "OldName" });
        var afterState = JsonSerializer.Serialize(new { Name = "NewName" });

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            BeforeState = beforeState,
            AfterState = afterState,
            OperationType = "update"
        };

        _mockRepository.Setup(r => r.LogAsync(
            It.IsAny<AuditLog>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLog);

        // Act
        var result = await _service.LogOperationAsync(
            Guid.NewGuid(),
            "system",
            "update",
            "permission",
            Guid.NewGuid(),
            beforeState,
            afterState);

        // Assert
        result.Should().NotBeNull();
        result.BeforeState.Should().Be(beforeState);
        result.AfterState.Should().Be(afterState);
    }

    /// <summary>
    /// 測試根據追蹤 ID 查詢稽核日誌
    /// </summary>
    [Fact]
    public async Task GetAuditLogsByTraceIdAsync_WithValidTraceId_ReturnsAuditLogs()
    {
        // Arrange
        var traceId = Guid.NewGuid().ToString();
        var auditLogs = new List<AuditLog>
        {
            new AuditLog { Id = Guid.NewGuid(), TraceId = traceId, OperationType = "create" },
            new AuditLog { Id = Guid.NewGuid(), TraceId = traceId, OperationType = "update" }
        };

        _mockRepository.Setup(r => r.GetByTraceIdAsync(traceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _service.GetAuditLogsByTraceIdAsync(traceId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// 測試查詢稽核日誌 - 空結果
    /// </summary>
    [Fact]
    public async Task GetAuditLogsAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var request = new QueryAuditLogRequest
        {
            PageNumber = 1,
            PageSize = 20,
            OperationType = "delete",
            TargetType = "nonexistent"
        };

        _mockRepository.Setup(r => r.GetLogsAsync(
            null, null, null, "delete", "nonexistent", null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog>(), 0L));

        // Act
        var result = await _service.GetAuditLogsAsync(request);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
