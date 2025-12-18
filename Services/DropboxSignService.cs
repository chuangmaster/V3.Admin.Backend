using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// Dropbox Sign API 服務
/// </summary>
/// <remarks>
/// 以 HttpClient 呼叫 Dropbox Sign API,提供線上簽名邀請寄送/狀態查詢/重新發送能力
/// </remarks>
public class DropboxSignService : IDropboxSignService
{
    private static readonly Uri _baseUri = new("https://api.hellosign.com/v3/");

    private readonly DropboxSignSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public DropboxSignService(IOptions<DropboxSignSettings> options, IHttpClientFactory httpClientFactory)
    {
        _settings = options.Value;
        _httpClientFactory = httpClientFactory;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("DROPBOX_SIGN_API_KEY 未設定");
        }
    }

    /// <summary>
    /// 發送線上簽名邀請
    /// </summary>
    /// <remarks>
    /// 為降低耦合,此方法以單一 PDF 作為示範；後續可擴充為一次送多份文件或合併文件後送出
    /// </remarks>
    public async Task<string> SendSignatureInvitationAsync(
        string recipientEmail,
        string documentName,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("recipientEmail 不可為空", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(documentName))
        {
            throw new ArgumentException("documentName 不可為空", nameof(documentName));
        }

        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("pdfBytes 不可為空", nameof(pdfBytes));
        }

        using HttpClient httpClient = CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "test_mode");
        content.Add(new StringContent(documentName), "title");
        content.Add(new StringContent(documentName), "subject");
        content.Add(new StringContent("請完成線上簽名"), "message");
        content.Add(new StringContent(recipientEmail), "signers[0][email_address]");
        content.Add(new StringContent("Customer"), "signers[0][name]");

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file[0]", "document.pdf");

        using HttpResponseMessage response = await httpClient.PostAsync(
            new Uri(_baseUri, "signature_request/send"),
            content,
            cancellationToken
        );

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Dropbox Sign API 呼叫失敗: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}"
            );
        }

        using JsonDocument doc = JsonDocument.Parse(responseText);

        string? requestId = doc.RootElement
            .GetProperty("signature_request")
            .GetProperty("signature_request_id")
            .GetString();

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new InvalidOperationException("Dropbox Sign 回傳缺少 signature_request_id");
        }

        return requestId;
    }

    /// <summary>
    /// 重新發送線上簽名邀請
    /// </summary>
    public async Task ResendSignatureInvitationAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("requestId 不可為空", nameof(requestId));
        }

        using HttpClient httpClient = CreateClient();

        var payload = JsonSerializer.Serialize(new { signature_request_id = requestId });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.PostAsync(
            new Uri(_baseUri, "signature_request/remind"),
            content,
            cancellationToken
        );

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Dropbox Sign API 呼叫失敗: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}"
            );
        }
    }

    /// <summary>
    /// 查詢線上簽名狀態
    /// </summary>
    public async Task<string> GetSignatureStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("requestId 不可為空", nameof(requestId));
        }

        using HttpClient httpClient = CreateClient();

        using HttpResponseMessage response = await httpClient.GetAsync(
            new Uri(_baseUri, $"signature_request/{Uri.EscapeDataString(requestId)}"),
            cancellationToken
        );

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Dropbox Sign API 呼叫失敗: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}"
            );
        }

        using JsonDocument doc = JsonDocument.Parse(responseText);

        string? isComplete = doc.RootElement
            .GetProperty("signature_request")
            .GetProperty("is_complete")
            .GetRawText();

        return isComplete == "true" ? "SIGNED" : "PENDING";
    }

    private HttpClient CreateClient()
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(nameof(DropboxSignService));
        httpClient.BaseAddress = _baseUri;

        string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiKey}:"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

        return httpClient;
    }
}
