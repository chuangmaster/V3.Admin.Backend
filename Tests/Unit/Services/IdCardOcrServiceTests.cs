using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Services;
using Xunit;

namespace V3.Admin.Backend.Tests.Unit.Services;

/// <summary>
/// 身分證 OCR 服務單元測試
/// </summary>
public class IdCardOcrServiceTests
{
    /// <summary>
    /// 當 Azure/Gemini 設定都缺失時，應以規則解析降級且回傳空結果
    /// </summary>
    [Fact]
    public async Task RecognizeAsync_WhenNoProvidersConfigured_ShouldReturnNullsWithZeroConfidence()
    {
        var azureOptions = Options.Create(new AzureVisionSettings { Endpoint = string.Empty, ApiKey = string.Empty });
        var geminiOptions = Options.Create(new GoogleGeminiSettings { ApiKey = string.Empty });

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var logger = new Mock<ILogger<IdCardOcrService>>();

        var service = new IdCardOcrService(azureOptions, geminiOptions, httpClientFactory.Object, logger.Object);

        var (name, idNumber, confidence) = await service.RecognizeAsync(new byte[] { 0x01 }, CancellationToken.None);

        name.Should().BeNull();
        idNumber.Should().BeNull();
        confidence.Should().Be(0);
    }

    /// <summary>
    /// 當 Gemini 可用且回傳有效 JSON 時，應回傳解析結果與合理信心度
    /// </summary>
    [Fact]
    public async Task RecognizeAsync_WhenGeminiReturnsValidResult_ShouldReturnParsedData()
    {
        var azureOptions = Options.Create(new AzureVisionSettings { Endpoint = string.Empty, ApiKey = string.Empty });
        var geminiOptions = Options.Create(new GoogleGeminiSettings
        {
            ApiKey = "fake-key",
            BaseUrl = "https://example.test/",
            Model = "fake-model",
            TimeoutSeconds = 10,
        });

        var handler = new FakeHttpMessageHandler(async request =>
        {
            var responsePayload = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = "{\"name\":\"王小明\",\"idNumber\":\"A123456789\",\"confidence\":0.9}",
                                },
                            },
                        },
                    },
                },
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responsePayload),
            };

            await Task.CompletedTask;
            return httpResponse;
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/"),
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var logger = new Mock<ILogger<IdCardOcrService>>();

        var service = new IdCardOcrService(azureOptions, geminiOptions, httpClientFactory.Object, logger.Object);

        // 只需要讓 DetectImageMimeType 能判斷為 PNG
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };

        var (name, idNumber, confidence) = await service.RecognizeAsync(pngBytes, CancellationToken.None);

        name.Should().Be("王小明");
        idNumber.Should().Be("A123456789");
        confidence.Should().BeApproximately(0.74, 0.0001);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            _ = cancellationToken;
            return _handler(request);
        }
    }
}
