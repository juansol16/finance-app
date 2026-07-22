namespace Cuintable.Server.Services;

/// <summary>
/// Gemini response schema shared by every advice call (per-statement and monthly),
/// so both produce the same shape the client parses with parseAdvice().
/// </summary>
public static class AdviceContract
{
    public const string Schema = """
    {
      "type": "OBJECT",
      "properties": {
        "summary": { "type": "STRING" },
        "suggestions": {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "title": { "type": "STRING" },
              "detail": { "type": "STRING" },
              "impactMXN": { "type": "NUMBER", "nullable": true }
            },
            "required": ["title", "detail"]
          }
        }
      },
      "required": ["summary", "suggestions"]
    }
    """;
}
