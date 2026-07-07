namespace Cuintable.Server.Services;

public interface IGeminiClient
{
    /// <summary>
    /// Calls Gemini generateContent with an enforced JSON response schema and
    /// returns the raw JSON text produced by the model.
    /// </summary>
    Task<string> GenerateJsonAsync(
        string model,
        string systemInstruction,
        string userPrompt,
        (byte[] Data, string MimeType)? inlineFile,
        string responseSchemaJson,
        CancellationToken cancellationToken = default);
}
