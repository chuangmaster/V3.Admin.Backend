using System.Data;
using System.Text.Json;
using V3.Admin.Backend.Models.Dtos;
using V3.Admin.Backend.Models.Entities;
using V3.Admin.Backend.Models.Requests;
using V3.Admin.Backend.Repositories.Interfaces;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 服務單服務實作
/// </summary>
public class ServiceOrderService : IServiceOrderService
{
    private const string _attachmentTypeIdCard = "ID_CARD";
    private const string _attachmentTypeContract = "CONTRACT";

    private readonly IDbConnection _dbConnection;
    private readonly ICustomerRepository _customerRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IProductItemRepository _productItemRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ISignatureRecordRepository _signatureRecordRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ServiceOrderService> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="dbConnection">資料庫連線</param>
    /// <param name="customerRepository">客戶資料存取層</param>
    /// <param name="serviceOrderRepository">服務單資料存取層</param>
    /// <param name="productItemRepository">商品項目資料存取層</param>
    /// <param name="attachmentRepository">附件資料存取層</param>
    /// <param name="signatureRecordRepository">簽名記錄資料存取層</param>
    /// <param name="blobStorageService">Blob Storage 服務</param>
    /// <param name="pdfGeneratorService">PDF 產生/簽名合併服務</param>
    /// <param name="auditLogService">稽核日誌服務</param>
    /// <param name="logger">日誌記錄器</param>
    public ServiceOrderService(
        IDbConnection dbConnection,
        ICustomerRepository customerRepository,
        IServiceOrderRepository serviceOrderRepository,
        IProductItemRepository productItemRepository,
        IAttachmentRepository attachmentRepository,
        ISignatureRecordRepository signatureRecordRepository,
        IBlobStorageService blobStorageService,
        IPdfGeneratorService pdfGeneratorService,
        IAuditLogService auditLogService,
        ILogger<ServiceOrderService> logger
    )
    {
        _dbConnection = dbConnection;
        _customerRepository = customerRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _productItemRepository = productItemRepository;
        _attachmentRepository = attachmentRepository;
        _signatureRecordRepository = signatureRecordRepository;
        _blobStorageService = blobStorageService;
        _pdfGeneratorService = pdfGeneratorService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// 產生收購合約預覽 PDF (US1)
    /// </summary>
    public async Task<byte[]> PreviewBuybackContractPdfAsync(
        CreateBuybackOrderRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fields = new Dictionary<string, string>
        {
            ["orderType"] = request.OrderType,
            ["orderSource"] = request.OrderSource,
            ["totalAmount"] = request.TotalAmount.ToString("0.##"),
            ["productItemCount"] = request.ProductItems.Count.ToString(),
        };

        if (request.CustomerId is not null)
        {
            fields["customerId"] = request.CustomerId.Value.ToString();
        }

        if (request.NewCustomer is not null)
        {
            fields["customerName"] = request.NewCustomer.Name;
            fields["customerPhone"] = request.NewCustomer.PhoneNumber;
            fields["customerEmail"] = request.NewCustomer.Email ?? string.Empty;
            fields["customerIdNumber"] = request.NewCustomer.IdNumber;
        }

        foreach (CreateBuybackProductItemRequest item in request.ProductItems)
        {
            fields[$"item[{item.SequenceNumber}].brand"] = item.BrandName;
            fields[$"item[{item.SequenceNumber}].style"] = item.StyleName;
            fields[$"item[{item.SequenceNumber}].internalCode"] = item.InternalCode ?? string.Empty;
        }

        _ = operatorId;

        return await _pdfGeneratorService.GeneratePreviewAsync(
            "BUYBACK_CONTRACT",
            fields,
            cancellationToken
        );
    }

    /// <summary>
    /// 合併 Base64 簽章至 PDF (US1)
    /// </summary>
    public async Task<byte[]> MergeSignatureAsync(
        MergeSignatureRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] pdfBytes;
        try
        {
            pdfBytes = Convert.FromBase64String(request.PdfBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("PdfBase64 格式不正確", ex);
        }

        return await _pdfGeneratorService.MergeSignatureAsync(
            pdfBytes,
            request.SignatureBase64Png,
            request.PageIndex,
            request.X,
            request.Y,
            request.Width,
            request.Height,
            cancellationToken
        );
    }

    /// <summary>
    /// 確認服務單並儲存最終 PDF (US1)
    /// </summary>
    public async Task<ConfirmOrderResultDto> ConfirmOrderAsync(
        Guid serviceOrderId,
        ConfirmOrderRequest request,
        Guid operatorId,
        CancellationToken cancellationToken = default
    )
    {
        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("serviceOrderId 不可為空", nameof(serviceOrderId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            throw new InvalidOperationException("DocumentType 不可為空");
        }

        if (string.IsNullOrWhiteSpace(request.PdfBase64))
        {
            throw new InvalidOperationException("PdfBase64 不可為空");
        }

        byte[] pdfBytes;
        try
        {
            pdfBytes = Convert.FromBase64String(request.PdfBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("PdfBase64 格式不正確", ex);
        }

        DateTime now = DateTime.UtcNow;

        ServiceOrder? serviceOrder = await _serviceOrderRepository.GetByIdAsync(
            serviceOrderId,
            cancellationToken
        );
        if (serviceOrder is null)
        {
            throw new KeyNotFoundException("服務單不存在");
        }

        string fileName = string.IsNullOrWhiteSpace(request.FileName)
            ? $"{request.DocumentType.ToUpperInvariant()}.pdf"
            : request.FileName;

        string safeFileName = GetSafeFileName(fileName, "application/pdf");
        string blobPath = BuildContractBlobPath(
            serviceOrderId,
            request.DocumentType,
            safeFileName,
            now
        );

        await EnsureConnectionOpenAsync(_dbConnection, cancellationToken);
        using IDbTransaction transaction = _dbConnection.BeginTransaction();

        try
        {
            using (var stream = new MemoryStream(pdfBytes, writable: false))
            {
                await _blobStorageService.UploadAsync(
                    blobPath,
                    stream,
                    "application/pdf",
                    cancellationToken
                );
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = serviceOrderId,
                AttachmentType = _attachmentTypeContract,
                FileName = safeFileName,
                BlobPath = blobPath,
                FileSize = pdfBytes.LongLength,
                ContentType = "application/pdf",
                CreatedAt = now,
                CreatedBy = operatorId,
                IsDeleted = false,
            };

            Attachment createdAttachment = await _attachmentRepository.CreateAsync(
                attachment,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            var signatureRecord = new SignatureRecord
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = serviceOrderId,
                DocumentType = request.DocumentType.Trim().ToUpperInvariant(),
                SignatureType = "OFFLINE",
                SignatureData = string.IsNullOrWhiteSpace(request.SignatureBase64Png)
                    ? null
                    : request.SignatureBase64Png,
                SignerName = string.IsNullOrWhiteSpace(request.SignerName)
                    ? string.Empty
                    : request.SignerName,
                SignedAt = now,
                CreatedAt = now,
                CreatedBy = operatorId,
            };

            SignatureRecord createdSignature = await _signatureRecordRepository.CreateAsync(
                signatureRecord,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            bool statusUpdated = await _serviceOrderRepository.UpdateStatusAsync(
                serviceOrderId,
                "COMPLETED",
                operatorId,
                serviceOrder.Version,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            if (!statusUpdated)
            {
                throw new InvalidOperationException("服務單狀態更新失敗 (可能為並發版本衝突)");
            }

            transaction.Commit();

            Uri sasUri = await _blobStorageService.GenerateReadSasUriAsync(
                blobPath,
                TimeSpan.FromHours(1),
                cancellationToken
            );

            return new ConfirmOrderResultDto
            {
                AttachmentId = createdAttachment.Id,
                SignatureRecordId = createdSignature.Id,
                BlobPath = blobPath,
                SasUri = sasUri,
                ExpiresAt = now.AddHours(1),
            };
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "確認服務單失敗後 rollback 交易時發生例外");
            }

            throw;
        }
    }

    /// <summary>
    /// 建立線下收購單 (US1)
    /// </summary>
    public async Task<ServiceOrderDetailDto> CreateBuybackOrderAsync(
        CreateBuybackOrderRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        DateTime now = DateTime.UtcNow;
        Customer customer = await ResolveCustomerAsync(request, createdBy, cancellationToken);

        Guid serviceOrderId = Guid.NewGuid();
        var serviceOrder = new ServiceOrder
        {
            Id = serviceOrderId,
            ServiceDate = now.Date,
            SequenceNumber = 0,
            OrderType = request.OrderType.ToUpperInvariant(),
            OrderSource = request.OrderSource.ToUpperInvariant(),
            CustomerId = customer.Id,
            TotalAmount = request.TotalAmount,
            Status = "PENDING",
            CreatedAt = now,
            CreatedBy = createdBy,
            IsDeleted = false,
            Version = 1,
        };

        List<ProductItem> productItems = request
            .ProductItems.Select(pi => new ProductItem
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = serviceOrderId,
                SequenceNumber = pi.SequenceNumber,
                BrandName = pi.BrandName,
                StyleName = pi.StyleName,
                InternalCode = pi.InternalCode,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        byte[] idCardBytes = Convert.FromBase64String(request.IdCardImageBase64);
        string idCardFileName = GetSafeFileName(
            request.IdCardImageFileName,
            request.IdCardImageContentType
        );
        string idCardBlobPath = BuildIdCardBlobPath(serviceOrderId, idCardFileName);

        ServiceOrder createdOrder;
        Attachment createdAttachment;
        List<SignatureRecord> createdSignatureRecords = new();
        bool createdNewCustomer = false;

        await EnsureConnectionOpenAsync(_dbConnection, cancellationToken);
        using IDbTransaction transaction = _dbConnection.BeginTransaction();

        try
        {
            if (request.NewCustomer is not null)
            {
                customer = await _customerRepository.CreateAsync(
                    customer,
                    transaction: transaction,
                    cancellationToken: cancellationToken
                );
                createdNewCustomer = true;
            }

            createdOrder = await _serviceOrderRepository.CreateAsync(
                serviceOrder,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            bool itemsCreated = await _productItemRepository.BatchCreateAsync(
                productItems,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            if (!itemsCreated)
            {
                throw new InvalidOperationException("建立商品項目失敗");
            }

            using (var stream = new MemoryStream(idCardBytes, writable: false))
            {
                await _blobStorageService.UploadAsync(
                    idCardBlobPath,
                    stream,
                    request.IdCardImageContentType,
                    cancellationToken
                );
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                ServiceOrderId = createdOrder.Id,
                AttachmentType = _attachmentTypeIdCard,
                FileName = idCardFileName,
                BlobPath = idCardBlobPath,
                FileSize = idCardBytes.LongLength,
                ContentType = request.IdCardImageContentType,
                CreatedAt = now,
                CreatedBy = createdBy,
                IsDeleted = false,
            };

            createdAttachment = await _attachmentRepository.CreateAsync(
                attachment,
                transaction: transaction,
                cancellationToken: cancellationToken
            );

            createdSignatureRecords.Add(
                await _signatureRecordRepository.CreateAsync(
                    new SignatureRecord
                    {
                        Id = Guid.NewGuid(),
                        ServiceOrderId = createdOrder.Id,
                        DocumentType = "BUYBACK_CONTRACT",
                        SignatureType = "OFFLINE",
                        SignatureData = null,
                        SignerName = customer.Name,
                        SignedAt = null,
                        CreatedAt = now,
                        CreatedBy = createdBy,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            );

            createdSignatureRecords.Add(
                await _signatureRecordRepository.CreateAsync(
                    new SignatureRecord
                    {
                        Id = Guid.NewGuid(),
                        ServiceOrderId = createdOrder.Id,
                        DocumentType = "ONE_TIME_TRADE",
                        SignatureType = "OFFLINE",
                        SignatureData = null,
                        SignerName = customer.Name,
                        SignedAt = null,
                        CreatedAt = now,
                        CreatedBy = createdBy,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            );

            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "建立收購單失敗後 rollback 交易時發生例外");
            }

            throw;
        }

        if (createdNewCustomer)
        {
            await TryLogCustomerCreatedAsync(customer, createdBy, cancellationToken);
        }

        await TryLogServiceOrderCreatedAsync(
            createdOrder,
            customer,
            createdBy,
            request,
            cancellationToken
        );

        return new ServiceOrderDetailDto
        {
            ServiceOrder = MapToDto(createdOrder),
            Customer = MapToDto(customer),
            ProductItems = productItems.Select(MapToDto).ToList(),
            Attachments = new List<AttachmentDto> { MapToDto(createdAttachment) },
            SignatureRecords = createdSignatureRecords.Select(MapToDto).ToList(),
        };
    }

    private async Task<Customer> ResolveCustomerAsync(
        CreateBuybackOrderRequest request,
        Guid createdBy,
        CancellationToken cancellationToken
    )
    {
        if (request.CustomerId is not null)
        {
            Customer? customer = await _customerRepository.GetByIdAsync(
                request.CustomerId.Value,
                cancellationToken
            );

            if (customer is null)
            {
                throw new KeyNotFoundException("客戶不存在");
            }

            return customer;
        }

        if (request.NewCustomer is null)
        {
            throw new InvalidOperationException("必須提供 CustomerId 或 NewCustomer");
        }

        Customer? existed = await _customerRepository.GetByIdNumberAsync(
            request.NewCustomer.IdNumber,
            cancellationToken
        );

        if (existed is not null)
        {
            throw new InvalidOperationException("身分證字號已存在,不可重複建立");
        }

        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.NewCustomer.Name,
            PhoneNumber = request.NewCustomer.PhoneNumber,
            Email = request.NewCustomer.Email,
            IdNumber = request.NewCustomer.IdNumber,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false,
            Version = 1,
        };
    }

    private static async Task EnsureConnectionOpenAsync(
        IDbConnection connection,
        CancellationToken cancellationToken
    )
    {
        if (connection.State == ConnectionState.Open)
        {
            return;
        }

        if (connection is System.Data.Common.DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
            return;
        }

        connection.Open();
    }

    private static string BuildIdCardBlobPath(Guid serviceOrderId, string safeFileName)
    {
        return $"service-orders/{serviceOrderId}/id-card/{Guid.NewGuid()}_{safeFileName}";
    }

    private static string BuildContractBlobPath(
        Guid serviceOrderId,
        string documentType,
        string safeFileName,
        DateTime utcNow
    )
    {
        string safeDocumentType = (documentType ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(safeDocumentType))
        {
            safeDocumentType = "CONTRACT";
        }

        string timestamp = utcNow.ToString("yyyyMMdd_HHmmss");
        return $"service-orders/{serviceOrderId}/contracts/{safeDocumentType}/{timestamp}_{Guid.NewGuid()}_{safeFileName}";
    }

    private static string GetSafeFileName(string fileName, string contentType)
    {
        string safeName = Path.GetFileName(fileName);
        if (!string.IsNullOrWhiteSpace(safeName))
        {
            return safeName;
        }

        return contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            ? "id-card.png"
            : "id-card.jpg";
    }

    private async Task TryLogCustomerCreatedAsync(
        Customer customer,
        Guid createdBy,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    customer.Id,
                    customer.Name,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.IdNumber,
                    customer.CreatedAt,
                }
            );

            await _auditLogService.LogOperationAsync(
                createdBy,
                "system",
                "create",
                "customer",
                customer.Id,
                beforeState: null,
                afterState: afterState,
                traceId: null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "記錄新增客戶稽核日誌失敗: CustomerId={CustomerId}", customer.Id);
        }
    }

    private async Task TryLogServiceOrderCreatedAsync(
        ServiceOrder order,
        Customer customer,
        Guid createdBy,
        CreateBuybackOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var afterState = JsonSerializer.Serialize(
                new
                {
                    order.Id,
                    order.OrderNumber,
                    order.OrderType,
                    order.OrderSource,
                    order.TotalAmount,
                    order.Status,
                    CustomerId = customer.Id,
                    request.ProductItems.Count,
                }
            );

            await _auditLogService.LogOperationAsync(
                createdBy,
                "system",
                "create",
                "service_order",
                order.Id,
                beforeState: null,
                afterState: afterState,
                traceId: null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "記錄建立收購單稽核日誌失敗: ServiceOrderId={ServiceOrderId}",
                order.Id
            );
        }
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            IdNumber = customer.IdNumber,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Version = customer.Version,
        };
    }

    private static ServiceOrderDto MapToDto(ServiceOrder serviceOrder)
    {
        return new ServiceOrderDto
        {
            Id = serviceOrder.Id,
            ServiceDate = serviceOrder.ServiceDate,
            SequenceNumber = serviceOrder.SequenceNumber,
            OrderNumber = serviceOrder.OrderNumber,
            OrderType = serviceOrder.OrderType,
            OrderSource = serviceOrder.OrderSource,
            CustomerId = serviceOrder.CustomerId,
            TotalAmount = serviceOrder.TotalAmount,
            Status = serviceOrder.Status,
            ConsignmentStartDate = serviceOrder.ConsignmentStartDate,
            ConsignmentEndDate = serviceOrder.ConsignmentEndDate,
            RenewalOption = serviceOrder.RenewalOption,
            CreatedAt = serviceOrder.CreatedAt,
            UpdatedAt = serviceOrder.UpdatedAt,
            Version = serviceOrder.Version,
        };
    }

    private static ProductItemDto MapToDto(ProductItem item)
    {
        return new ProductItemDto
        {
            Id = item.Id,
            ServiceOrderId = item.ServiceOrderId,
            SequenceNumber = item.SequenceNumber,
            BrandName = item.BrandName,
            StyleName = item.StyleName,
            InternalCode = item.InternalCode,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }

    private static AttachmentDto MapToDto(Attachment attachment)
    {
        return new AttachmentDto
        {
            Id = attachment.Id,
            ServiceOrderId = attachment.ServiceOrderId,
            AttachmentType = attachment.AttachmentType,
            FileName = attachment.FileName,
            BlobPath = attachment.BlobPath,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            CreatedAt = attachment.CreatedAt,
            IsDeleted = attachment.IsDeleted,
        };
    }

    private static SignatureRecordDto MapToDto(SignatureRecord record)
    {
        return new SignatureRecordDto
        {
            Id = record.Id,
            ServiceOrderId = record.ServiceOrderId,
            DocumentType = record.DocumentType,
            SignatureType = record.SignatureType,
            SignatureData = record.SignatureData,
            DropboxSignRequestId = record.DropboxSignRequestId,
            DropboxSignStatus = record.DropboxSignStatus,
            DropboxSignUrl = record.DropboxSignUrl,
            SignerName = record.SignerName,
            SignedAt = record.SignedAt,
            CreatedAt = record.CreatedAt,
        };
    }
}
