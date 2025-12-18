using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Options;
using V3.Admin.Backend.Configuration;
using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Services;

/// <summary>
/// 身分證 OCR 辨識服務
/// </summary>
/// <remarks>
/// 目前以可運作的降級策略為主：先嘗試 Azure Vision 擷取文字(若可用),再以規則解析姓名/身分證字號
/// 後續可將「規則解析」替換為 Google Gemini 結構化解析,並以加權方式計算信心度
/// </remarks>
public class IdCardOcrService : IIdCardOcrService
{
    private static readonly Regex _taiwanIdRegex = new(
        "[A-Z][0-9]{9}",
        RegexOptions.Compiled
    );
    private static readonly Regex _foreignerIdRegex = new(
        "[0-9]{8}[A-Z]{2}",
        RegexOptions.Compiled
    );
    private static readonly Regex _jsonObjectRegex = new(
        "\\{[\\s\\S]*\\}",
        RegexOptions.Compiled
    );

    private readonly AzureVisionSettings _azureVisionSettings;
    private readonly GoogleGeminiSettings _googleGeminiSettings;
    private readonly ILogger<IdCardOcrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public IdCardOcrService(
        IOptions<AzureVisionSettings> azureVisionOptions,
        IOptions<GoogleGeminiSettings> googleGeminiOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<IdCardOcrService> logger
    )
    {
        _azureVisionSettings = azureVisionOptions.Value;
        _googleGeminiSettings = googleGeminiOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 辨識身分證姓名與身分證字號
    /// </summary>
    /// <remarks>
    /// 規格要求雙服務並用且具降級機制；在尚未完成 Gemini SDK 串接前,先以文字規則解析保障基本可用
    /// </remarks>
    public async Task<(string? Name, string? IdNumber, double Confidence)> RecognizeAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken = default
    )
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            throw new ArgumentException("imageBytes 不可為空", nameof(imageBytes));
        }

        string? extractedText = null;

        // 嘗試 Azure Vision OCR (若設定齊全)
        if (!string.IsNullOrWhiteSpace(_azureVisionSettings.Endpoint)
            && !string.IsNullOrWhiteSpace(_azureVisionSettings.ApiKey))
        {
            try
            {
                extractedText = await TryExtractTextByAzureAsync(imageBytes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Vision OCR 失敗，將降級以文字規則解析");
            }
        }
        else
        {
            _logger.LogWarning("Azure Vision 設定未齊全，將降級以文字規則解析");
        }

        // 若 OCR 服務不可用，仍可嘗試由上層傳入的替代文字(此版本未支援)；因此此處允許空字串
        extractedText ??= string.Empty;

        // 1) 優先使用 Gemini 將 OCR 結果結構化(含姓名/證號/信心度)
        if (!string.IsNullOrWhiteSpace(_googleGeminiSettings.ApiKey))
        {
            try
            {
                GeminiIdCardResult? geminiResult = await TryParseByGeminiAsync(
                    extractedText,
                    imageBytes,
                    cancellationToken
                );

                if (geminiResult is not null)
                {
                    var normalizedId = NormalizeIdNumber(geminiResult.IdNumber);
                    var isValidId = IsValidTaiwanId(normalizedId) || IsValidForeignerId(normalizedId);
                    var name = NormalizeName(geminiResult.Name);

                    double confidence = CalculateConfidence(
                        extractedText,
                        name,
                        normalizedId,
                        isValidId,
                        geminiResult.Confidence
                    );

                    return (name, normalizedId, confidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Gemini 解析失敗，將降級以規則解析");
            }
        }
        else
        {
            _logger.LogWarning("Google Gemini 設定未齊全，暫以規則解析取代");
        }

        // 2) 降級：以規則解析 OCR 文字
        var fallbackId = NormalizeIdNumber(ExtractIdNumber(extractedText));
        var fallbackName = NormalizeName(ExtractNameBestEffort(extractedText));
        var fallbackValidId = IsValidTaiwanId(fallbackId) || IsValidForeignerId(fallbackId);
        var fallbackConfidence = CalculateConfidence(
            extractedText,
            fallbackName,
            fallbackId,
            fallbackValidId,
            null
        );

        return (fallbackName, fallbackId, fallbackConfidence);
    }

    private async Task<GeminiIdCardResult?> TryParseByGeminiAsync(
        string ocrText,
        byte[] imageBytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_googleGeminiSettings.BaseUrl))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_googleGeminiSettings.Model))
        {
            return null;
        }

        // 以 OCR 文字優先，若文字為空則改用圖片(若可辨識 MIME)
        bool useImage = string.IsNullOrWhiteSpace(ocrText);
        string? mimeType = useImage ? DetectImageMimeType(imageBytes) : null;
        if (useImage && mimeType is null)
        {
            return null;
        }

        string prompt = BuildGeminiPrompt(ocrText, useImage);

        using HttpClient httpClient = _httpClientFactory.CreateClient(nameof(IdCardOcrService));
        httpClient.BaseAddress = new Uri(_googleGeminiSettings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(
            Math.Clamp(_googleGeminiSettings.TimeoutSeconds, 3, 60)
        );

        string requestPath =
            $"v1beta/models/{Uri.EscapeDataString(_googleGeminiSettings.Model)}:generateContent?key={Uri.EscapeDataString(_googleGeminiSettings.ApiKey)}";

        object payload = useImage
            ? new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType,
                                    data = Convert.ToBase64String(imageBytes),
                                },
                            },
                        },
                    },
                },
                generationConfig = new
                {
                    temperature = 0,
                    maxOutputTokens = 256,
                },
            }
            : new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = prompt },
                        },
                    },
                },
                generationConfig = new
                {
                    temperature = 0,
                    maxOutputTokens = 256,
                },
            };

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestPath,
            payload,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Google Gemini API 呼叫失敗: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}"
            );
        }

        JsonDocument? json = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken
        );
        if (json is null)
        {
            return null;
        }

        string? text = ExtractGeminiText(json);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string? jsonObjectText = ExtractJsonObjectText(text);
        if (string.IsNullOrWhiteSpace(jsonObjectText))
        {
            return null;
        }

        return ParseGeminiResult(jsonObjectText);
    }

    private static string BuildGeminiPrompt(string ocrText, bool useImage)
    {
        var sb = new StringBuilder();

        sb.AppendLine("你是一個負責解析台灣身分證(或居留證)資料的系統。請嚴格輸出 JSON，不要輸出任何多餘文字。\n");
        sb.AppendLine("輸出格式：{\"name\":string|null,\"idNumber\":string|null,\"confidence\":number}，confidence 為 0 到 1。\n");
        sb.AppendLine("規則：");
        sb.AppendLine("- idNumber 請輸出大寫且移除空白與符號。");
        sb.AppendLine("- 若無法判斷請輸出 null。");
        sb.AppendLine("- confidence 反映你對輸出的信心(越確定越高)。\n");

        if (!useImage)
        {
            sb.AppendLine("以下是 OCR 擷取的文字，請解析姓名與身分證/居留證號：");
            sb.AppendLine(ocrText);
        }
        else
        {
            sb.AppendLine("請從提供的影像解析姓名與身分證/居留證號。");
        }

        return sb.ToString();
    }

    private static string? ExtractGeminiText(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("candidates", out JsonElement candidates))
        {
            return null;
        }

        if (candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement first = candidates[0];
        if (!first.TryGetProperty("content", out JsonElement content))
        {
            return null;
        }

        if (!content.TryGetProperty("parts", out JsonElement parts))
        {
            return null;
        }

        if (parts.ValueKind != JsonValueKind.Array || parts.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement part0 = parts[0];
        if (!part0.TryGetProperty("text", out JsonElement text))
        {
            return null;
        }

        return text.GetString();
    }

    private static string? ExtractJsonObjectText(string text)
    {
        Match match = _jsonObjectRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        return match.Value;
    }

    private static GeminiIdCardResult? ParseGeminiResult(string jsonObjectText)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonObjectText);
            JsonElement root = doc.RootElement;

            string? name = root.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() : null;
            string? idNumber = root.TryGetProperty("idNumber", out JsonElement idEl) ? idEl.GetString() : null;

            double? confidence = null;
            if (root.TryGetProperty("confidence", out JsonElement confEl))
            {
                if (confEl.ValueKind == JsonValueKind.Number && confEl.TryGetDouble(out double parsed))
                {
                    confidence = parsed;
                }
            }

            return new GeminiIdCardResult(name, idNumber, confidence);
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return null;
        }

        return new string(idNumber
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return name.Trim();
    }

    private static double CalculateConfidence(
        string ocrText,
        string? name,
        string? idNumber,
        bool isValidId,
        double? geminiConfidence
    )
    {
        double score = 0;

        if (!string.IsNullOrWhiteSpace(ocrText))
        {
            score += 0.25;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            score += 0.15;
        }

        if (!string.IsNullOrWhiteSpace(idNumber))
        {
            score += 0.30;
        }

        if (isValidId)
        {
            score += 0.20;
        }

        if (geminiConfidence.HasValue)
        {
            score += Math.Clamp(geminiConfidence.Value, 0, 1) * 0.10;
        }

        return Math.Clamp(score, 0, 1);
    }

    private static bool IsValidForeignerId(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return false;
        }

        return _foreignerIdRegex.IsMatch(idNumber);
    }

    private static bool IsValidTaiwanId(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return false;
        }

        if (!_taiwanIdRegex.IsMatch(idNumber))
        {
            return false;
        }

        // 台灣身分證驗證：
        // 字母轉數字：A=10, B=11 ... Z=35
        // 加權：X1*1 + X2*9 + d1*8 + d2*7 + d3*6 + d4*5 + d5*4 + d6*3 + d7*2 + d8*1 + d9*1
        // 總和 % 10 == 0
        int code = idNumber[0] - 'A' + 10;
        int x1 = code / 10;
        int x2 = code % 10;

        int sum = x1 * 1 + x2 * 9;

        for (int i = 1; i <= 9; i++)
        {
            int digit = idNumber[i] - '0';

            int weight = i switch
            {
                1 => 8,
                2 => 7,
                3 => 6,
                4 => 5,
                5 => 4,
                6 => 3,
                7 => 2,
                8 => 1,
                9 => 1,
                _ => 0,
            };

            sum += digit * weight;
        }

        return sum % 10 == 0;
    }

    private static string? DetectImageMimeType(byte[] imageBytes)
    {
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (imageBytes.Length >= 8
            && imageBytes[0] == 0x89
            && imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47
            && imageBytes[4] == 0x0D
            && imageBytes[5] == 0x0A
            && imageBytes[6] == 0x1A
            && imageBytes[7] == 0x0A)
        {
            return "image/png";
        }

        // JPEG: FF D8
        if (imageBytes.Length >= 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
        {
            return "image/jpeg";
        }

        return null;
    }

    private sealed record GeminiIdCardResult(string? Name, string? IdNumber, double? Confidence);

    private static string? ExtractIdNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string upper = text.ToUpperInvariant();

        Match taiwanMatch = _taiwanIdRegex.Match(upper);
        if (taiwanMatch.Success)
        {
            return taiwanMatch.Value;
        }

        Match foreignerMatch = _foreignerIdRegex.Match(upper);
        if (foreignerMatch.Success)
        {
            return foreignerMatch.Value;
        }

        return null;
    }

    private static string? ExtractNameBestEffort(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // 以「姓名」關鍵字做簡易擷取；若格式多樣,建議改用 Gemini 解析
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string line in lines)
        {
            if (line.Contains("姓名", StringComparison.OrdinalIgnoreCase))
            {
                var value = line
                    .Replace("姓名", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace(":", string.Empty)
                    .Replace("：", string.Empty)
                    .Trim();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private Task<string> TryExtractTextByAzureAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var endpoint = new Uri(_azureVisionSettings.Endpoint);
        var credential = new AzureKeyCredential(_azureVisionSettings.ApiKey);
        var client = new ImageAnalysisClient(endpoint, credential);

        // Read: OCR 文字擷取
        ImageAnalysisResult result = client.Analyze(
            BinaryData.FromBytes(imageBytes),
            VisualFeatures.Read
        );

        var lines = result.Read?.Blocks
            .SelectMany(b => b.Lines)
            .Select(l => l.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        return Task.FromResult(lines is null ? string.Empty : string.Join("\n", lines));
    }
}
