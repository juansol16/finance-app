using System.Text;
using System.Text.Json;

namespace Cuintable.Server.Services;

public class GeminiClient : IGeminiClient
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public GeminiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _apiKey = configuration["Gemini:ApiKey"];
    }

    public async Task<string> GenerateJsonAsync(
        string model,
        string systemInstruction,
        string userPrompt,
        (byte[] Data, string MimeType)? inlineFile,
        string responseSchemaJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Gemini:ApiKey is not configured. Set GEMINI_API_KEY in .env.");

        var parts = new List<object>();
        if (inlineFile is not null)
        {
            parts.Add(new
            {
                inlineData = new
                {
                    mimeType = inlineFile.Value.MimeType,
                    data = Convert.ToBase64String(inlineFile.Value.Data)
                }
            });
        }
        parts.Add(new { text = userPrompt });

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = parts.ToArray() } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = JsonSerializer.Deserialize<JsonElement>(responseSchemaJson)
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{model}:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 500 ? body[..500] : body;
            throw new InvalidOperationException($"Gemini API error ({(int)response.StatusCode}) for model '{model}': {snippet}");
        }

        return ExtractText(body, model);
    }

    /// <summary>Pulls the generated text out of a generateContent response body.</summary>
    public static string ExtractText(string responseBody, string model)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            var reason = root.TryGetProperty("promptFeedback", out var feedback) &&
                         feedback.TryGetProperty("blockReason", out var block)
                ? block.GetString()
                : "no candidates returned";
            throw new InvalidOperationException($"Gemini ({model}) returned no content: {reason}");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            var finish = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "unknown";
            throw new InvalidOperationException($"Gemini ({model}) response has no parts (finishReason: {finish}).");
        }

        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var t))
                text.Append(t.GetString());
        }

        if (text.Length == 0)
            throw new InvalidOperationException($"Gemini ({model}) response contained no text parts.");

        return text.ToString();
    }
}
