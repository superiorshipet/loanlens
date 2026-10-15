namespace LoanLens.Infrastructure.Configuration;

public class AiAdvisorOptions
{
    public const string SectionName = "AiAdvisor";

    /// <summary>
    /// Preferred primary provider: "Gemini" or "Groq".
    /// </summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>
    /// Google Gemini API key.
    /// </summary>
    public string? GeminiApiKey { get; set; }

    /// <summary>
    /// Google Gemini Model (e.g. "gemini-3.5-flash").
    /// </summary>
    public string GeminiModelId { get; set; } = "gemini-3.5-flash";

    /// <summary>
    /// Groq API key (starts with "gsk_").
    /// </summary>
    public string? GroqApiKey { get; set; }

    /// <summary>
    /// Groq Model (e.g. "openai/gpt-oss-120b" or "allam-2-7b").
    /// </summary>
    public string GroqModelId { get; set; } = "openai/gpt-oss-120b";

    /// <summary>
    /// Temperature for LLM inference (0.2 - 0.4 recommended for factual consistency).
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
