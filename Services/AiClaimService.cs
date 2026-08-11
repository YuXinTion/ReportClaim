using System.Text;
using System.Text.Json;
using InsuranceClaimApi.Models;

namespace InsuranceClaimApi.Services;

public class AiClaimService : IAiClaimService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AiClaimService> _logger;

    public AiClaimService(HttpClient httpClient, IConfiguration config, ILogger<AiClaimService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ClaimAssessmentResult> AssessClaimAsync(
        IFormFile passport,
        IFormFile boardingPass,
        List<IFormFile> baggageImages,
        string description,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ClaimAssessmentResult
            {
                Error = "Gemini API key is not configured. Set Gemini:ApiKey in appsettings.json or user-secrets."
            };
        }

        // Build the multimodal "parts" array: text prompt first, then each image as inline_data.
        var parts = new List<object>
        {
            new { text = BuildPrompt(description) }
        };

        parts.Add(await ToImagePartAsync(passport, cancellationToken));
        parts.Add(await ToImagePartAsync(boardingPass, cancellationToken));

        foreach (var img in baggageImages)
        {
            parts.Add(await ToImagePartAsync(img, cancellationToken));
        }

        var requestBody = new
        {
            contents = new object[]
            {
                new { role = "user", parts }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.2
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} {Body}", response.StatusCode, responseText);
                return new ClaimAssessmentResult
                {
                    Error = $"AI service returned an error: {response.StatusCode}. {responseText}"
                };
            }

            using var doc = JsonDocument.Parse(responseText);

            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                return new ClaimAssessmentResult { Error = "AI returned no candidates. The image may have been blocked by safety filters." };
            }

            var messageContent = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return new ClaimAssessmentResult { Error = "AI returned an empty response." };
            }

            return ParseAiResponse(messageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call AI service");
            return new ClaimAssessmentResult { Error = "Failed to reach AI service: " + ex.Message };
        }
    }

    private static string BuildPrompt(string description)
    {
        return $@"
Analyze the attached passport image, boarding pass image, and one or more damaged baggage images
for a travel insurance baggage damage claim.

Claim description provided by the customer:
""{description}""

Return ONLY a single valid JSON object (no markdown, no code fences, no extra text) with EXACTLY these fields:
{{
  ""customerName"": string (from passport),
  ""passportNo"": string (from passport),
  ""flightNo"": string (from boarding pass),
  ""damageDetected"": boolean (true if the baggage images show visible damage),
  ""severity"": ""Low"" | ""Medium"" | ""High"",
  ""recommendation"": ""Approve"" | ""Reject"" | ""Needs Investigation"",
  ""confidence"": number between 0 and 1,
  ""summary"": a 2-3 sentence plain-English summary of the claim and your reasoning
}}

If any field cannot be confidently read from the images, make your best estimate and lower the confidence score accordingly.
";
    }

    private static async Task<object> ToImagePartAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());
        var mimeType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;

        return new
        {
            inline_data = new
            {
                mime_type = mimeType,
                data = base64
            }
        };
    }

    private ClaimAssessmentResult ParseAiResponse(string rawJson)
    {
        try
        {
            var cleaned = rawJson.Trim();
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Trim('`');
                var newlineIdx = cleaned.IndexOf('\n');
                if (newlineIdx > 0) cleaned = cleaned[(newlineIdx + 1)..];
            }

            var result = JsonSerializer.Deserialize<ClaimAssessmentResult>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ClaimAssessmentResult { Error = "AI response could not be parsed." };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI JSON response: {Raw}", rawJson);
            return new ClaimAssessmentResult
            {
                Error = "AI returned malformed JSON and could not be parsed automatically.",
                Summary = rawJson.Length > 300 ? rawJson[..300] + "..." : rawJson
            };
        }
    }
}