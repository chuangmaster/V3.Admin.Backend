using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services;
using V3.Admin.Backend.Services.Interfaces;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 服務單服務單元測試
/// </summary>
public class ServiceOrderServiceTests
{
    private readonly Mock<IDbConnection> _dbConnection;
    private readonly Mock<ICustomerRepository> _customerRepository;
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepository;
    private readonly Mock<IProductItemRepository> _productItemRepository;
    private readonly Mock<IAttachmentRepository> _attachmentRepository;
    private readonly Mock<ISignatureRecordRepository> _signatureRecordRepository;
    private readonly Mock<IBlobStorageService> _blobStorageService;
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorService;
    private readonly Mock<IAuditLogService> _auditLogService;
    private readonly Mock<ILogger<ServiceOrderService>> _logger;
    private readonly ServiceOrderService _service;

    public ServiceOrderServiceTests()
    {
        _dbConnection = new Mock<IDbConnection>();
        _customerRepository = new Mock<ICustomerRepository>();
        _serviceOrderRepository = new Mock<IServiceOrderRepository>();
        _productItemRepository = new Mock<IProductItemRepository>();
        _attachmentRepository = new Mock<IAttachmentRepository>();
        _signatureRecordRepository = new Mock<ISignatureRecordRepository>();
        _blobStorageService = new Mock<IBlobStorageService>();
        _pdfGeneratorService = new Mock<IPdfGeneratorService>();
        _auditLogService = new Mock<IAuditLogService>();
        _logger = new Mock<ILogger<ServiceOrderService>>();

        _service = new ServiceOrderService(
            _dbConnection.Object,
            _customerRepository.Object,
            _serviceOrderRepository.Object,
            _productItemRepository.Object,
            _attachmentRepository.Object,
            _signatureRecordRepository.Object,
            _blobStorageService.Object,
            _pdfGeneratorService.Object,
            _auditLogService.Object,
            _logger.Object
        );
    }

    /// <summary>
    /// 產生合約預覽時應呼叫 PdfGeneratorService
    /// </summary>
    [Fact]
    public async Task PreviewBuybackContractPdfAsync_ShouldCallPdfGeneratorService()
    {
        var operatorId = Guid.NewGuid();
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            NewCustomer = new CreateCustomerRequest
            {
                Name = "王小明",
                PhoneNumber = "0912345678",
                IdNumber = "A123456789",
            },
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new() { SequenceNumber = 1, BrandName = "CHANEL", StyleName = "Classic" },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = Convert.ToBase64String(new byte[] { 1 }),
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id.png",
        };

        byte[] expected = [1, 2, 3];
        _pdfGeneratorService
            .Setup(s => s.GeneratePreviewAsync(
                "BUYBACK_CONTRACT",
                It.Is<Dictionary<string, string>>(d => d["orderType"] == "BUYBACK" && d["orderSource"] == "OFFLINE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        byte[] result = await _service.PreviewBuybackContractPdfAsync(request, operatorId, CancellationToken.None);

        result.Should().Equal(expected);
    }

    /// <summary>
    /// 合併簽章時 PdfBase64 無法解析應丟出例外
    /// </summary>
    [Fact]
    public async Task MergeSignatureAsync_WithInvalidPdfBase64_ShouldThrow()
    {
        var request = new MergeSignatureRequest
        {
            PdfBase64 = "not-base64",
            SignatureBase64Png = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            PageIndex = 0,
            X = 10,
            Y = 10,
            Width = 100,
            Height = 50,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.MergeSignatureAsync(request, CancellationToken.None)
        );
    }

    /// <summary>
    /// 確認服務單時，若服務單不存在應丟出 KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task ConfirmOrderAsync_WhenServiceOrderNotFound_ShouldThrow()
    {
        var serviceOrderId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var request = new ConfirmOrderRequest
        {
            DocumentType = "BUYBACK_CONTRACT",
            PdfBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            SignerName = "王小明",
        };

        _serviceOrderRepository
            .Setup(r => r.GetByIdAsync(serviceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ConfirmOrderAsync(serviceOrderId, request, operatorId, CancellationToken.None)
        );
    }

    /// <summary>
    /// 確認服務單成功應回傳附件與 SAS 資訊
    /// </summary>
    [Fact]
    public async Task ConfirmOrderAsync_WithValidRequest_ShouldReturnResult()
    {
        var serviceOrderId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        _serviceOrderRepository
            .Setup(r => r.GetByIdAsync(serviceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceOrder
            {
                Id = serviceOrderId,
                CustomerId = Guid.NewGuid(),
                OrderType = "BUYBACK",
                OrderSource = "OFFLINE",
                SequenceNumber = 1,
                ServiceDate = DateTime.UtcNow.Date,
                TotalAmount = 1000,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                IsDeleted = false,
            });

        _dbConnection.SetupGet(c => c.State).Returns(ConnectionState.Closed);
        _dbConnection.Setup(c => c.Open());

        var transaction = new Mock<IDbTransaction>();
        _dbConnection.Setup(c => c.BeginTransaction()).Returns(transaction.Object);

        _blobStorageService
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, Stream _, string __, CancellationToken ___) => path);

        _attachmentRepository
            .Setup(r => r.CreateAsync(It.IsAny<Attachment>(), transaction.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attachment att, IDbTransaction? _, CancellationToken __) => att);

        _signatureRecordRepository
            .Setup(r => r.CreateAsync(It.IsAny<SignatureRecord>(), transaction.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignatureRecord record, IDbTransaction? _, CancellationToken __) => record);

        var sasUri = new Uri("https://example.test/sas");
        _blobStorageService
            .Setup(s => s.GenerateReadSasUriAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sasUri);

        var request = new ConfirmOrderRequest
        {
            DocumentType = "BUYBACK_CONTRACT",
            PdfBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            SignerName = "王小明",
        };

        var result = await _service.ConfirmOrderAsync(serviceOrderId, request, operatorId, CancellationToken.None);

        result.AttachmentId.Should().NotBe(Guid.Empty);
        result.SignatureRecordId.Should().NotBe(Guid.Empty);
        result.BlobPath.Should().StartWith($"service-orders/{serviceOrderId}/contracts/");
        result.SasUri.Should().Be(sasUri);

        transaction.Verify(t => t.Commit(), Times.Once);
    }

    /// <summary>
    /// 建立收購單時，若 CustomerId 找不到客戶應丟出 KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task CreateBuybackOrderAsync_WhenCustomerNotFound_ShouldThrow()
    {
        var operatorId = Guid.NewGuid();
        var request = new CreateBuybackOrderRequest
        {
            OrderType = "BUYBACK",
            OrderSource = "OFFLINE",
            CustomerId = Guid.NewGuid(),
            ProductItems = new List<CreateBuybackProductItemRequest>
            {
                new() { SequenceNumber = 1, BrandName = "CHANEL", StyleName = "Classic" },
            },
            TotalAmount = 1000,
            IdCardImageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            IdCardImageContentType = "image/png",
            IdCardImageFileName = "id.png",
        };

        _customerRepository
            .Setup(r => r.GetByIdAsync(request.CustomerId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateBuybackOrderAsync(request, operatorId, CancellationToken.None)
        );
    }
}
